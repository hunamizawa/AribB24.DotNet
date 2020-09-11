namespace AribB24.DotNet
{
    /// <summary>
    /// 文字符号集合を表す列挙体。
    /// </summary>
    enum GraphicSet : ushort
    {
        //None = 0x00,
        /// <summary>漢字系集合 04/2</summary>
        Kanji = 0x42,
        /// <summary>英数集合 04/10</summary>
        Alphanumeric = 0x4A,
        /// <summary>平仮名集合 03/0</summary>
        Hiragana = 0x30,
        /// <summary>片仮名集合 03/1</summary>
        Katakana = 0x31,
        /// <summary>モザイク A 集合 03/2</summary>
        MosaicA = 0x32,
        /// <summary>モザイク B 集合 03/3</summary>
        MosaicB = 0x33,
        /// <summary>モザイク C 集合 03/4</summary>
        MosaicC = 0x34,
        /// <summary>モザイク D 集合 03/5</summary>
        MosaicD = 0x35,
        /// <summary>プロポーショナル英数集合 03/6</summary>
        ProportionalAlphanumeric = 0x36,
        /// <summary>プロポーショナル平仮名集合 03/7</summary>
        ProportionalHiragana = 0x37,
        /// <summary>プロポーショナル片仮名集合 03/8</summary>
        ProportionalKatakana = 0x38,
        /// <summary>JIS X0201 片仮名集合 04/9</summary>
        JISX0201Katakana = 0x49,
        /// <summary>JIS 互換漢字 1 面集合 03/9</summary>
        JISCompatibleKanji_Plane1 = 0x39,
        /// <summary>JIS 互換漢字 2 面集合 03/10</summary>
        JISCompatibleKanji_Plane2 = 0x3A,
        /// <summary>追加記号集合 03/11</summary>
        AdditionalSymbols = 0x3B,
        DRCS0 = 0x2040,
        DRCS1 = 0x2041,
        DRCS2 = 0x2042,
        DRCS3 = 0x2043,
        DRCS4 = 0x2044,
        DRCS5 = 0x2045,
        DRCS6 = 0x2046,
        DRCS7 = 0x2047,
        DRCS8 = 0x2048,
        DRCS9 = 0x2049,
        DRCS10 = 0x204A,
        DRCS11 = 0x204B,
        DRCS12 = 0x204C,
        DRCS13 = 0x204D,
        DRCS14 = 0x204E,
        DRCS15 = 0x204F,
        Macro = 0x2070,
    }

    static class GraphicSetExtensions
    {
        /// <summary>
        /// 文字符号集合が 2 バイト集合かどうか判定する。
        /// </summary>
        public static bool Is2bytesSet(this GraphicSet set)
        {
            return set == GraphicSet.Kanji
                || set == GraphicSet.JISCompatibleKanji_Plane1
                || set == GraphicSet.JISCompatibleKanji_Plane2
                || set == GraphicSet.AdditionalSymbols
                || set == GraphicSet.DRCS0;
        }

        public static bool IsAlphanumeric(this GraphicSet set)
        {
            return set == GraphicSet.Alphanumeric || set == GraphicSet.ProportionalAlphanumeric;
        }

        public static bool IsHiragana(this GraphicSet set)
        {
            return set == GraphicSet.Hiragana || set == GraphicSet.ProportionalHiragana;
        }

        public static bool IsKatakana(this GraphicSet set)
        {
            return set == GraphicSet.Katakana || set == GraphicSet.ProportionalKatakana;
        }
    }
}
