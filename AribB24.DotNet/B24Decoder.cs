﻿using System;
using System.Text;

namespace AribB24.DotNet
{
    /// <summary>
    /// ARIB STD-B24 8単位符号 をデコードするクラス。
    /// </summary>
    public partial class B24Decoder
    {
#if !NETSTANDARD2_0
        private const int MAX_STACKALLOC_BYTES = 128;
#endif

        // こいつらは文字符号集合に Windows-31J を使っている
        private static readonly Encoding sjis;
        private static readonly Encoding eucjp;

        static B24Decoder()
        {
            // SJIS, EUC-JP を利用可能にする
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            sjis = Encoding.GetEncoding(932);
            eucjp = Encoding.GetEncoding(51932);
        }

        private static bool IsCx(byte value) => (value & 0b_0110_0000) == 0;
        private static bool IsCL(byte value) => (value & 0b_1110_0000) == 0;
        private static bool IsGL(byte value) => (value & 0b_1000_0000) == 0 && !IsCx(value);
        private static bool IsCR(byte value) => (value & 0b_1110_0000) == 0b_1000_0000;
        private static bool IsGR(byte value) => (value & 0b_1000_0000) != 0 && !IsCx(value);

        /// <summary>
        /// byte 配列 <paramref name="bytes"/> に格納されているすべてのバイトを文字列にデコード
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public string GetString(byte[] bytes)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

#if NETSTANDARD2_0
            return GetString(bytes, 0, bytes.Length);
#else
            return GetString(bytes.AsSpan());
#endif
        }

        /// <summary>
        /// byte 配列 <paramref name="bytes"/> のうち、<paramref name="index"/> 番目から <paramref name="count"/> バイトを文字列にデコード
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public string GetString(byte[] bytes, int index, int count)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));
            
            if (bytes.Length <= index)
                throw new ArgumentOutOfRangeException(nameof(index));

            var startOfBytes = index;
            var endOfBytes = index + count;
            if (bytes.Length < endOfBytes)
                throw new ArgumentOutOfRangeException(nameof(count));

#if !NETSTANDARD2_0
            return GetString(bytes.AsSpan(index, count));
        }

        /// <summary>
        /// byte 列 <paramref name="bytes"/> に格納されているすべてのバイトを文字列にデコード
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public string GetString(ReadOnlySpan<byte> bytes)
        {
            var startOfBytes = 0;
            var endOfBytes = bytes.Length;
#endif
            var sb = new StringBuilder(endOfBytes - startOfBytes);

            var g = new GraphicSet[] // 符号指示の初期状態
            {
                GraphicSet.JISCompatibleKanji_Plane1, // TODO: 漢字集合かもしれない
                GraphicSet.Alphanumeric,
                GraphicSet.Hiragana,
                GraphicSet.Katakana, // TODO: マクロ符号集合かもしれない
            };
            var gl = 0;                // GL に呼び出されている符号集合
            var gr = 2;                // GR に呼び出されている符号集合
            var glSingleShift = -1;    // GLシングルシフトの状態
            var halfwidthFlag = false; // [MSZ] が指示されているかどうか

            for (int i = startOfBytes; i < endOfBytes; i++)
            {
                var current = bytes[i];

                var currentIsGL = IsGL(current);
                var isGLSS = glSingleShift != -1;
                var glSet = isGLSS ? g[glSingleShift] : g[gl];

                var currentIsEUC = IsEUC(glSet, g[gr], bytes, i);
                if (currentIsEUC != DecodeCondition.Never)
                {
                    var (str, consumed) = ProcessAsEUC(bytes, i, endOfBytes, currentIsGL, currentIsEUC, halfwidthFlag);
                    if (str is null)
                        break;
                    sb.Append(str);
                    i += consumed - 1;

                    if (currentIsGL && isGLSS)
                        glSingleShift = -1;
                    continue;
                }

                var currentIsCP932 = IsCP932(glSet, g[gr], bytes, i);
                if (currentIsCP932 != DecodeCondition.Never)
                {
                    var str = ProcessAsCP932(bytes, i, currentIsGL, currentIsCP932, halfwidthFlag);
                    if (str is null)
                        break;
                    sb.Append(str);
                    if (currentIsGL && isGLSS)
                        glSingleShift = -1;
                    continue;
                }

                if (currentIsGL || IsGR(current))
                {
                    var (str, consumed) = ProcessGx(bytes, i, endOfBytes, currentIsGL ? glSet : g[gr], halfwidthFlag);
                    if (str is null)
                        break;
                    sb.Append(str);
                    i += consumed - 1;
                }
                else
                {
                    if (!ProcessControlCodes(bytes, sb, endOfBytes, g, ref gl, ref gr, ref glSingleShift, ref halfwidthFlag, ref i))
                        break;
                }
            }

            return sb.ToString();
        }

        private static (string? result, int consumedBytes) ProcessAsEUC(
#if NETSTANDARD2_0
            byte[]             bytes,
#else
            ReadOnlySpan<byte> bytes,
#endif
            int i,
            int count,
            bool currentIsGL,
            DecodeCondition currentIsEUC,
            bool isHalfwidth)
        {
            var current = bytes[i];

            // EUC-JP の GL 領域は ASCII 固定
            var isASCII = (currentIsGL && currentIsEUC == DecodeCondition.RequireNoConv) ||
                currentIsEUC == DecodeCondition.RequireConvToGL;
            if (isASCII)
            {
                var isYenSign = (current & 0b_0111_1111) == 0x5C;
                if (isYenSign)
                    return (isHalfwidth ? "\u00A5" : "\uFFE5", 1);

                string str;
                if (currentIsGL)
                {
#if NETSTANDARD2_0
                    str = eucjp.GetString(bytes, i, 1);
#else
                    str = eucjp.GetString(bytes.Slice(i, 1));
#endif
                }
                else
                {
#if NETSTANDARD2_0
                    byte[] buffer = new byte[]
#else
                    Span<byte> buffer = stackalloc byte[]
#endif
                    {
                        (byte)(current & 0b_0111_1111)
                    };
                    str = eucjp.GetString(buffer);
                }

                if (!isHalfwidth && halfToFullTable.TryGetValue(str, out var fullwidth))
                    return (fullwidth, 1);
                else
                    return (str, 1);
            }
            else
            {
                if (++i >= count)
                    return (null, 1);
                var next = bytes[i];

                string str;
                if (currentIsGL)
                {
#if NETSTANDARD2_0
                    byte[] buffer = new byte[]
#else
                    Span<byte> buffer = stackalloc byte[]
#endif
                    {
                        (byte)(current | 0b_1000_0000),
                        (byte)(next    | 0b_1000_0000)
                    };
                    str = eucjp.GetString(buffer);
                }
                else
                {
#if NETSTANDARD2_0
                    str = eucjp.GetString(bytes, i, 2);
#else
                    str = eucjp.GetString(bytes.Slice(i, 2));
#endif
                }

                if (isHalfwidth && fullToHalfTable.TryGetValue(str, out var halfwidth))
                    return (halfwidth, 2);
                else
                    return (str, 2);
            }
        }


        private static string? ProcessAsCP932(
#if NETSTANDARD2_0
            byte[]             bytes,
#else
            ReadOnlySpan<byte> bytes,
#endif
            int i,
            bool currentIsGL,
            DecodeCondition currentIsCP932,
            bool isHalfwidth)
        {
            var current = bytes[i];

            // 円記号の特別処理
            var isYenSign = (currentIsCP932 == DecodeCondition.RequireNoConv   && current == 0x5C) ||
                            (currentIsCP932 == DecodeCondition.RequireConvToGL && current == 0xDC);
            if (isYenSign)
                return isHalfwidth ? "\u00A5" : "\uFFE5";

            string str;
            if (currentIsCP932 == DecodeCondition.RequireNoConv)
            {
#if NETSTANDARD2_0
                str = sjis.GetString(bytes, i, 1);
#else
                str = sjis.GetString(bytes.Slice(i, 1));
#endif
            }
            else
            {
#if NETSTANDARD2_0
                byte[] buffer = new byte[]
#else
                Span<byte> buffer = stackalloc byte[]
#endif
                {
                    currentIsCP932 is DecodeCondition.RequireConvToGL ?
                        (byte)(current & 0b_0111_1111) :
                        (byte)(current | 0b_1000_0000)
                };
                str = sjis.GetString(buffer);
            }
            if (!isHalfwidth && halfToFullTable.TryGetValue(str, out var fullwidth))
                return fullwidth;
            else
                return str;
        }

        private static (string? result, int consumedBytes) ProcessGx(
#if NETSTANDARD2_0
            byte[]             bytes,
#else
            ReadOnlySpan<byte> bytes,
#endif
            int i,
            int count,
            GraphicSet set,
            bool isHalfwidth)
        {
            var current = bytes[i] & 0b_0111_1111;
            var currentIsGL = IsGL(bytes[i]);

            if (set.Is2bytesSet())
            {
                if (++i >= count)
                    return (null, 1);

                var next = bytes[i] & 0b_0111_1111;

                if (currentIsGL != IsGL(bytes[i]))
                    throw new InvalidOperationException("IsGL(current) != IsGL(next)");

                var code = (current << 8) + next;
                return (LookupChar(set, code, isHalfwidth), 2);
            }

            if (set == GraphicSet.JISX0201Katakana && !isHalfwidth && i + 1 < count)
            {
                var next = bytes[i] & 0b_0111_1111;
                if (next == 0x5E || next == 0x5F)
                {
                    // 次の文字が濁点・半濁点のとき、全角では1文字に結合できるのでそうする
                    var code = (current << 8) + next;
                    return (LookupChar(set, code, isHalfwidth), 2);
                }
            }

            return (LookupChar(set, current, isHalfwidth), 1);
        }

        private enum Pane
        {
            GL,
            GR
        }

        /// <summary>
        /// <paramref name="i"/> から始まるバイト列は EUC-JP として解釈できる？　解釈するにあたって GL/GR の変換は必要？
        /// </summary>
        /// <param name="glSet">GL はこの集合が呼び出されてるはず、という仮定条件</param>
        /// <param name="grSet">GR はこの集合が呼び出されてるはず、という仮定条件</param>
#if NETSTANDARD2_0
        private static DecodeCondition IsEUC(GraphicSet glSet, GraphicSet grSet, byte[]             bytes, int i)
#else
        private static DecodeCondition IsEUC(GraphicSet glSet, GraphicSet grSet, ReadOnlySpan<byte> bytes, int i)
#endif
        {
            if (i >= bytes.Length)
                return DecodeCondition.Never;

            var current = bytes[i];
            if (current == C0.SP || current == C0.DEL)
                return DecodeCondition.RequireNoConv;
            if (IsCx(current))
                return DecodeCondition.Never;

            var currentIsGL = IsGL(current);
            var currentIsGR = !currentIsGL; // CR はもう除かれてる
            if (currentIsGL && glSet.IsAlphanumeric())
                return DecodeCondition.RequireNoConv;
            if (currentIsGR && grSet.IsAlphanumeric())
                return DecodeCondition.RequireConvToGL;

            var currentIsKanji = (currentIsGL && (glSet == GraphicSet.JISCompatibleKanji_Plane1 || glSet == GraphicSet.Kanji)) ||
                                 (currentIsGR && (grSet == GraphicSet.JISCompatibleKanji_Plane1 || grSet == GraphicSet.Kanji));
            if (currentIsKanji)
            {
                if (i + 1 >= bytes.Length)
                    return DecodeCondition.Never;

                var next = bytes[i + 1];
                if (IsCx(next) || currentIsGL != IsGL(next))
                    return DecodeCondition.Never;

                // JIS X 0213:2004 に含まれるけど CP51932 ではデコードできない文字を
                // ちまちまと取り除いていく……

                // 区番号
                var ku = current & 0b_0111_1111;

                // ARIB 漢字系集合の 13 区には文字が何もない
                var currentSet = currentIsGL ? glSet : grSet;
                if (currentSet == GraphicSet.Kanji && ku == 0x2D)
                    return DecodeCondition.Never;

                // 点番号
                var ten = next & 0b_0111_1111;

                if (!CanParseEUCJP(ku, ten))
                    return DecodeCondition.Never;

                return currentIsGR ? DecodeCondition.RequireNoConv : DecodeCondition.RequireConvToGR;
            }
            else
            {
                return DecodeCondition.Never;
            }
        }

        /// <summary>
        /// <paramref name="i"/> から始まるバイト列は CP932 (Shift JIS) として解釈できる？　解釈するにあたって GL/GR の変換は必要？
        /// </summary>
        /// <param name="glSet">GL はこの集合が呼び出されてるはず、という仮定条件</param>
        /// <param name="grSet">GR はこの集合が呼び出されてるはず、という仮定条件</param>
#if NETSTANDARD2_0
        private static DecodeCondition IsCP932(GraphicSet glSet, GraphicSet grSet, byte[]             bytes, int i)
#else
        private static DecodeCondition IsCP932(GraphicSet glSet, GraphicSet grSet, ReadOnlySpan<byte> bytes, int i)
#endif
        {
            if (i >= bytes.Length)
                return DecodeCondition.Never;

            var current = bytes[i];
            if (current == C0.SP || current == C0.DEL)
                return DecodeCondition.RequireNoConv;
            if (IsCx(current))
                return DecodeCondition.Never;

            var currentIsGL = IsGL(current);
            var currentIsGR = !currentIsGL;
            if (currentIsGL && glSet.IsAlphanumeric())
                return DecodeCondition.RequireNoConv;
            if (currentIsGR && grSet.IsAlphanumeric())
                return DecodeCondition.RequireConvToGL;
            if (currentIsGL && glSet == GraphicSet.JISX0201Katakana)
                return DecodeCondition.RequireConvToGR;
            if (currentIsGR && grSet == GraphicSet.JISX0201Katakana)
                return DecodeCondition.RequireNoConv;

            return DecodeCondition.Never;
        }

        private static bool ProcessControlCodes(
#if NETSTANDARD2_0
            byte[]             bytes,
#else
            ReadOnlySpan<byte> bytes,
#endif
            StringBuilder sb,
            int endOfBytes,
            GraphicSet[] g,
            ref int gl,
            ref int gr,
            ref int glSingleShift,
            ref bool halfwidthFlag,
            ref int i)
        {
            // return false は大域脱出（forループ終了）の意

            var current = bytes[i];

            switch (current)
            {
                case C0.APB:
                    sb.Append('\b');
                    break;
                case C0.APF:
                    sb.Append('\t');
                    break;
                case C0.APD:
                case C0.APR:
                    sb.AppendLine();
                    break;
                case C0.LS1:
                    gl = 1;
                    break;
                case C0.LS0:
                    gl = 0;
                    break;
                case C0.PAPF:
                    if (++i >= endOfBytes) return false; // この制御文字を無視してパラメーターを読み飛ばす
                                                         // もし末尾ならforループ終了
                    break;
                case C0.SS2:
                    glSingleShift = 2;
                    break;
                case C0.ESC:
                    if (++i >= endOfBytes) return false;
                    var next = bytes[i];
                    switch (next)
                    {
                        case 0x24:
                            {
                                if (++i >= endOfBytes) return false;
                                var third = bytes[i];
                                switch (third)
                                {
                                    case 0x28:
                                        // ESC 02/4 02/8 → ESC 02/4 02/8 02/0 F のパターンで確定
                                        // 2バイトDRCSを G0 に指示 → 2バイトDRCS集合は1種類しかない
                                        i += 2;
                                        if (i >= endOfBytes) return false;
                                        g[0] = GraphicSet.DRCS0;
                                        break;
                                    case 0x29:
                                    case 0x2A:
                                    case 0x2B:
                                        {
                                            if (++i >= endOfBytes) return false;
                                            var fourth = bytes[i];

                                            var buf = third & 0x03; // G0 -> 02/8 ... G3 -> 02/11 なので 0x28 を引けばバッファの番号が求まる。
                                                                    // 0x28 == 0b00101000 なので、X の下位2ビットは (X - 0x28) に等しい。

                                            if (fourth == 0x20)
                                            {
                                                // ESC 02/4 02/x 02/0 F
                                                // 2バイトDRCSの指示
                                                if (++i >= endOfBytes) return false;
                                                g[buf] = GraphicSet.DRCS0;
                                                break;
                                            }
                                            // ESC 02/4 02/x F
                                            // 2バイトGセットの指示
                                            g[buf] = (GraphicSet)fourth;
                                        }
                                        break;
                                    default:
                                        // ESC 02/4 F
                                        // 2バイトGセットを G0 に指示
                                        g[0] = (GraphicSet)third;
                                        break;
                                }
                            }
                            break;
                        case 0x28:
                        case 0x29:
                        case 0x2A:
                        case 0x2B:
                            {
                                if (++i >= endOfBytes) return false;
                                var third = bytes[i];
                                var buf = next & 0x03;

                                if (third == 0x20)
                                {
                                    // ESC 02/x 02/0 F
                                    // 1バイトDRCSの指示
                                    if (++i >= endOfBytes) return false;
                                    g[buf] = (GraphicSet)(0x2000 | bytes[i]); // (0x2000 | X) == (0x2000 + X)
                                }
                                else
                                {
                                    // ESC 02/x F
                                    // 1バイトGセットの指示
                                    g[buf] = (GraphicSet)third;
                                }
                            }
                            break;
                        case 0x6E: // LS2
                            gl = 2;
                            break;
                        case 0x6F: // LS3
                            gl = 3;
                            break;
                        case 0x7C: // LS3R
                            gr = 3;
                            break;
                        case 0x7D: // LS2R
                            gr = 2;
                            break;
                        case 0x7E: // LS1R
                            gr = 1;
                            break;
                        default:
                            break;
                    }
                    break;
                case C0.APS:
                    i += 2;
                    if (i >= endOfBytes) return false;
                    break;
                case C0.SS3:
                    glSingleShift = 3;
                    break;
                case C0.SP:
                    if (halfwidthFlag)
                        sb.Append('\x20'); // 半角空白
                    else
                        sb.Append('\u3000'); // 全角空白
                    break;
                // TODO: DEL の使いみちがよくわからない
                //case C0.DEL:
                //    throw new NotImplementedException();
                case C1.MSZ:
                    halfwidthFlag = true;
                    break;
                case C1.NSZ:
                    halfwidthFlag = false;
                    break;
                default:
                    break;
            }

            return true;
        }

        /// <summary>
        /// <paramref name="set"/> と <paramref name="code"/> の組に対応する文字を取得
        /// </summary>
        /// <param name="set"></param>
        /// <param name="code"></param>
        /// <param name="halfwidthFlag"></param>
        /// <returns></returns>
        private static string LookupChar(GraphicSet set, int code, bool halfwidthFlag)
        {
            static string convToHalfIf(bool halfwidthFlag, string full)
            {
                if (halfwidthFlag && fullToHalfTable.TryGetValue(full, out var half))
                    return half;
                return full;
            }

            return set switch
            {
                GraphicSet.Kanji
                    => throw new InvalidOperationException("漢字系集合の振り分けに失敗している"),

                GraphicSet.JISCompatibleKanji_Plane1
                    => convToHalfIf(halfwidthFlag, jisx0213Plane1Table[code]),

                GraphicSet.JISCompatibleKanji_Plane2
                    => jisx0213Plane2Table[code],

                GraphicSet.AdditionalSymbols
                    => additionalSymbolTable[code],

                GraphicSet.Alphanumeric or GraphicSet.ProportionalAlphanumeric
                    => convToHalfIf(halfwidthFlag, jisx0201FullwidthTable[code]),

                GraphicSet.Hiragana or GraphicSet.ProportionalHiragana
                    => hiraganaTable[code],

                GraphicSet.Katakana or GraphicSet.ProportionalKatakana
                    => convToHalfIf(halfwidthFlag, katakanaTable[code]),

                GraphicSet.JISX0201Katakana
                    => convToHalfIf(halfwidthFlag, jisx0201FullwidthTable[code]),

                _ => throw new InvalidOperationException($"未定義の {nameof(GraphicSet)}"),
            };
        }
    }

    enum DecodeCondition
    {
        Never,
        RequireNoConv,
        RequireConvToGL,
        RequireConvToGR,
    }

    static class ByteExtension
    {
        public static byte ConvertAs(this byte value, DecodeCondition cond)
        {
            return cond switch
            {
                DecodeCondition.RequireNoConv => value,
                DecodeCondition.RequireConvToGL => (byte)(value & 0b_0111_1111),
                DecodeCondition.RequireConvToGR => (byte)(value | 0b_1000_0000),
                DecodeCondition.Never => throw new InvalidOperationException(),
                _ => throw new InvalidOperationException($"未定義の {nameof(DecodeCondition)}"),
            };
        }
    }
}
