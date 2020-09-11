using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace AribB24.DotNet.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void SimpleDecodeTest()
        {
            var encoder = new B24Decoder();

            // TODO
            // なんかバイト列を与える
            // デコードできればおｋ
            var bytes = new byte[] { 0x0E, 0x1B, 0x7C, 0xA2, 0xA4, 0x89, 0xA6, 0xA8, 0x41, 0x42, 0x8A, 0x43, 0x44 };
            var expected = "アイｳｴABＣＤ";

            var actual = encoder.GetString(bytes);
            Assert.Equal(expected, actual);

            // エラーになるパターンも試す
        }

        [Fact]
        public void Equals_HalfToFullTable_And_FullToHalfTable()
        {
            foreach (var (half, full) in B24Decoder.halfToFullTable)
                Assert.Equal(half, B24Decoder.fullToHalfTable[full]);
            foreach (var (full, half) in B24Decoder.fullToHalfTable)
                Assert.Equal(full, B24Decoder.halfToFullTable[half]);
        }

        [Fact]
        public void Decodes_JIS_X_0213_2004_Collectly()
        {
            var (jisCodes, fullToHalf) = ReadJisX0213File(@"jisx0213-2004-8bit-std.txt");
            ReadZen2HanFile(@"zen2han.txt", fullToHalf);

            var encoder = new B24Decoder();

            var bs = new byte[] {
                0x1b, 0x28, 0x39, // (GL <-) G0 <- JIS互換漢字1面
                0x1b, 0x2a, 0x3a, // (GR <-) G2 <- JIS互換漢字2面
                0x00, 0x00,       // 1文字目
                0x89,             // NSZ
                0x00, 0x00        // 2文字目
            };

            foreach (var (kuTen, codePoints) in jisCodes)
            {
                var expected_full = CodepointsToString(codePoints);
                var expected_half = fullToHalf.GetValueOrDefault(expected_full) ?? expected_full;

                // 特別パターン
                var expected = expected_full switch
                {
                    "～" => "～~",
                    _ => expected_full + expected_half,
                };

                var ku = (byte)(kuTen >> 8);
                var ten = (byte)(kuTen & 0xFF);
                bs[6] = bs[9] = ku;
                bs[7] = bs[10] = ten;

                var actual = encoder.GetString(bs);
                //Assert.AreEqual(expected, actual, "Expected:<{0}>. Actual:<{1}>. 0x{2:X4} ({2}区{3}点)", AsUnicodeLiteral(expected), AsUnicodeLiteral(actual), kuTen, ku - 0x20, ten - 0x20);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void Decodes_Katakana_Hiragana_Collectly()
        {
            var patterns = new Dictionary<int, (string, string, string)>
            {
                [0x21] = ("ァ", "ｧ", "ぁ"),
                [0x22] = ("ア", "ｱ", "あ"),
                [0x23] = ("ィ", "ｨ", "ぃ"),
                [0x24] = ("イ", "ｲ", "い"),
                [0x25] = ("ゥ", "ｩ", "ぅ"),
                [0x26] = ("ウ", "ｳ", "う"),
                [0x27] = ("ェ", "ｪ", "ぇ"),
                [0x28] = ("エ", "ｴ", "え"),
                [0x29] = ("ォ", "ｫ", "ぉ"),
                [0x2A] = ("オ", "ｵ", "お"),
                [0x2B] = ("カ", "ｶ", "か"),
                [0x2C] = ("ガ", "ｶﾞ", "が"),
                [0x2D] = ("キ", "ｷ", "き"),
                [0x2E] = ("ギ", "ｷﾞ", "ぎ"),
                [0x2F] = ("ク", "ｸ", "く"),
                [0x30] = ("グ", "ｸﾞ", "ぐ"),
                [0x31] = ("ケ", "ｹ", "け"),
                [0x32] = ("ゲ", "ｹﾞ", "げ"),
                [0x33] = ("コ", "ｺ", "こ"),
                [0x34] = ("ゴ", "ｺﾞ", "ご"),
                [0x35] = ("サ", "ｻ", "さ"),
                [0x36] = ("ザ", "ｻﾞ", "ざ"),
                [0x37] = ("シ", "ｼ", "し"),
                [0x38] = ("ジ", "ｼﾞ", "じ"),
                [0x39] = ("ス", "ｽ", "す"),
                [0x3A] = ("ズ", "ｽﾞ", "ず"),
                [0x3B] = ("セ", "ｾ", "せ"),
                [0x3C] = ("ゼ", "ｾﾞ", "ぜ"),
                [0x3D] = ("ソ", "ｿ", "そ"),
                [0x3E] = ("ゾ", "ｿﾞ", "ぞ"),
                [0x3F] = ("タ", "ﾀ", "た"),
                [0x40] = ("ダ", "ﾀﾞ", "だ"),
                [0x41] = ("チ", "ﾁ", "ち"),
                [0x42] = ("ヂ", "ﾁﾞ", "ぢ"),
                [0x43] = ("ッ", "ｯ", "っ"),
                [0x44] = ("ツ", "ﾂ", "つ"),
                [0x45] = ("ヅ", "ﾂﾞ", "づ"),
                [0x46] = ("テ", "ﾃ", "て"),
                [0x47] = ("デ", "ﾃﾞ", "で"),
                [0x48] = ("ト", "ﾄ", "と"),
                [0x49] = ("ド", "ﾄﾞ", "ど"),
                [0x4A] = ("ナ", "ﾅ", "な"),
                [0x4B] = ("ニ", "ﾆ", "に"),
                [0x4C] = ("ヌ", "ﾇ", "ぬ"),
                [0x4D] = ("ネ", "ﾈ", "ね"),
                [0x4E] = ("ノ", "ﾉ", "の"),
                [0x4F] = ("ハ", "ﾊ", "は"),
                [0x50] = ("バ", "ﾊﾞ", "ば"),
                [0x51] = ("パ", "ﾊﾟ", "ぱ"),
                [0x52] = ("ヒ", "ﾋ", "ひ"),
                [0x53] = ("ビ", "ﾋﾞ", "び"),
                [0x54] = ("ピ", "ﾋﾟ", "ぴ"),
                [0x55] = ("フ", "ﾌ", "ふ"),
                [0x56] = ("ブ", "ﾌﾞ", "ぶ"),
                [0x57] = ("プ", "ﾌﾟ", "ぷ"),
                [0x58] = ("ヘ", "ﾍ", "へ"),
                [0x59] = ("ベ", "ﾍﾞ", "べ"),
                [0x5A] = ("ペ", "ﾍﾟ", "ぺ"),
                [0x5B] = ("ホ", "ﾎ", "ほ"),
                [0x5C] = ("ボ", "ﾎﾞ", "ぼ"),
                [0x5D] = ("ポ", "ﾎﾟ", "ぽ"),
                [0x5E] = ("マ", "ﾏ", "ま"),
                [0x5F] = ("ミ", "ﾐ", "み"),
                [0x60] = ("ム", "ﾑ", "む"),
                [0x61] = ("メ", "ﾒ", "め"),
                [0x62] = ("モ", "ﾓ", "も"),
                [0x63] = ("ャ", "ｬ", "ゃ"),
                [0x64] = ("ヤ", "ﾔ", "や"),
                [0x65] = ("ュ", "ｭ", "ゅ"),
                [0x66] = ("ユ", "ﾕ", "ゆ"),
                [0x67] = ("ョ", "ｮ", "ょ"),
                [0x68] = ("ヨ", "ﾖ", "よ"),
                [0x69] = ("ラ", "ﾗ", "ら"),
                [0x6A] = ("リ", "ﾘ", "り"),
                [0x6B] = ("ル", "ﾙ", "る"),
                [0x6C] = ("レ", "ﾚ", "れ"),
                [0x6D] = ("ロ", "ﾛ", "ろ"),
                [0x6E] = ("ヮ", "ヮ", "ゎ"),
                [0x6F] = ("ワ", "ﾜ", "わ"),
                [0x70] = ("ヰ", "ヰ", "ゐ"),
                [0x71] = ("ヱ", "ヱ", "ゑ"),
                [0x72] = ("ヲ", "ｦ", "を"),
                [0x73] = ("ン", "ﾝ", "ん"),
                [0x74] = ("ヴ", "ｳﾞ", null),
                [0x75] = ("ヵ", "ヵ", null),
                [0x76] = ("ヶ", "ヶ", null),
                [0x77] = ("ヽ", "ヽ", "ゝ"),
                [0x78] = ("ヾ", "ヾ", "ゞ"),
                [0x79] = ("ー", "ｰ", "ー"),
                [0x7A] = ("。", "｡", "。"),
                [0x7B] = ("「", "｢", "「"),
                [0x7C] = ("」", "｣", "」"),
                [0x7D] = ("、", "､", "、"),
                [0x7E] = ("・", "･", "・"),
            };
            var encoder = new B24Decoder();

            var bs = new byte[] {
                0x1b, 0x28, 0x31, // (GL <-) G0 <- カタカナ集合
                0x1b, 0x2a, 0x30, // (GR <-) G2 <- ひらがな集合
                0x00,             // 1文字目
                0x00,             // 2文字目
                0x89,             // NSZ
                0x00,             // 3文字目（半角カナ）
            };

            foreach (var (code, v) in patterns)
            {
                var expected = (v.Item3 ?? v.Item1) + v.Item1 + v.Item2;
                bs[6] = (byte)(v.Item3 is null ? code : (code + 0x80));
                bs[7] = bs[9] = (byte)code;
                var actual = encoder.GetString(bs);
                //Assert.AreEqual(expected, actual, "Expected:<{0}>. Actual:<{1}>. 0x{2:X2}", AsUnicodeLiteral(expected), AsUnicodeLiteral(actual), code);
                Assert.Equal(expected, actual);
            }
        }

        /// <summary>
        /// "jisx0213-2004-8bit-std.txt" をパースする
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static (IReadOnlyDictionary<int, int[]> jisCodes, Dictionary<string, string> fullToHalf) ReadJisX0213File(string file)
        {
            var jisCodes = new Dictionary<int, int[]>();
            var fullToHalf = new Dictionary<string, string>();

            foreach (var line in File.ReadLines(file, Encoding.UTF8))
            {
                if (!line.Any() || line[0] == '#')
                    continue;

                var cols = line.Split('\t');

                if (cols.Length < 2 || cols[1].Length == 0)
                    continue;

                var kuTen = int.Parse(cols[0].Substring(2), NumberStyles.HexNumber);
                var windows = cols.FirstOrDefault(p => p.StartsWith("Windows: U+"));
                var fullwidth = cols.FirstOrDefault(p => p.StartsWith("Fullwidth: U+"));
                var codePoints = ParseCodePoints(windows ?? fullwidth ?? cols[1]);

                jisCodes.Add(kuTen, codePoints);
                if (fullwidth != null)
                {
                    var full = CodepointsToString(codePoints);
                    var half = CodepointsToString(ParseCodePoints(cols[1]));
                    if (fullToHalf.ContainsKey(full))
                        fullToHalf[full] = half;
                    else
                        fullToHalf.Add(full, half);
                }
            }

            return (jisCodes, fullToHalf);
        }

        /// <summary>
        /// "zen2han.txt" をパースする
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fullToHalf"></param>
        private void ReadZen2HanFile(string file, Dictionary<string, string> fullToHalf)
        {
            foreach (var line in File.ReadLines(file, Encoding.UTF8))
            {
                if (!line.Any() || line[0] == '#')
                    continue;

                var cols = line.Split('\t');

                if (cols.Length < 2)
                    continue;

                var full = cols[0];
                var half = cols[1];
                if (fullToHalf.ContainsKey(full))
                    fullToHalf[full] = half;
                else
                    fullToHalf.Add(full, half);
            }
        }

        /// <summary>
        /// "U+aaaa+bbbb" のように表現されているコードポイント列を、int[] { 0xaaaa, 0xbbbb } に変換
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        private static int[] ParseCodePoints(string cp)
        {
            return cp.Split('+')
                .Skip(1)
                .Select(p => int.Parse(p, NumberStyles.HexNumber))
                .ToArray();
        }

        /// <summary>
        /// int 型のコードポイント列を string に変換
        /// </summary>
        /// <param name="codePoints"></param>
        /// <returns></returns>
        private static string CodepointsToString(IEnumerable<int> codePoints)
        {
            return string.Join("", codePoints.Select(p => char.ConvertFromUtf32(p)));
        }

        private static string AsUnicodeLiteral(string str)
        {
            return string.Join("", str.Select(p => $"\\u{(int)p:X4}"));
        }
    }
}
