using System;
using System.Collections.Generic;
using System.Text;

namespace AribB24.DotNet
{
    public partial class B24Decoder
    {
        private static class C0
        {
            /// <summary>空白（ヌル文字）</summary>
            internal const byte NUL = 0x00;
            /// <summary>ベル</summary>
            internal const byte BEL = 0x07;
            /// <summary>動作位置後退</summary>
            internal const byte APB = 0x08;
            /// <summary>動作位置前進</summary>
            internal const byte APF = 0x09;
            /// <summary>動作行前進</summary>
            internal const byte APD = 0x0A;
            /// <summary>動作行後退</summary>
            internal const byte APU = 0x0B;
            /// <summary>画面消去</summary>
            internal const byte CS = 0x0C;
            /// <summary>動作位置改行</summary>
            internal const byte APR = 0x0D;
            /// <summary>ロッキングシフト 1</summary>
            internal const byte LS1 = 0x0E;
            /// <summary>ロッキングシフト 0</summary>
            internal const byte LS0 = 0x0F;
            /// <summary>指定動作位置前進<br />
            /// パラメーター 1byte</summary>
            internal const byte PAPF = 0x16;
            /// <summary>キャンセル</summary>
            internal const byte CAN = 0x18;
            /// <summary>シングルシフト 2</summary>
            internal const byte SS2 = 0x19;
            /// <summary>エスケープ</summary>
            internal const byte ESC = 0x1B;
            /// <summary>動作位置指定<br />パラメーター 2bytes</summary>
            internal const byte APS = 0x1C;
            /// <summary>シングルシフト 3</summary>
            internal const byte SS3 = 0x1D;
            /// <summary>データヘッダ識別符号</summary>
            internal const byte RS = 0x1E;
            /// <summary>データユニット識別符号</summary>
            internal const byte US = 0x1F;

            internal const byte SP = 0x20;
            internal const byte DEL = 0x7F;
        }

        private static class C1
        {
            /// <summary>前景色黒およびカラーマップ下位アドレス指定</summary>
            internal const byte BKF = 0x80;
            /// <summary>前景色赤およびカラーマップ下位アドレス指定</summary>
            internal const byte RDF = 0x81;
            /// <summary>前景色緑およびカラーマップ下位アドレス指定</summary>
            internal const byte GRF = 0x82;
            /// <summary>前景色黄およびカラーマップ下位アドレス指定</summary>
            internal const byte YLF = 0x83;
            /// <summary>前景色青およびカラーマップ下位アドレス指定</summary>
            internal const byte BLF = 0x84;
            /// <summary>前景色マゼンタおよびカラーマップ下位アドレス指定</summary>
            internal const byte MGF = 0x85;
            /// <summary>前景色シアンおよびカラーマップ下位アドレス指定</summary>
            internal const byte CNF = 0x86;
            /// <summary>前景色白およびカラーマップ下位アドレス指定</summary>
            internal const byte WHF = 0x87;
            /// <summary>小型サイズ</summary>
            internal const byte SSZ = 0x88;
            /// <summary>中型サイズ</summary>
            internal const byte MSZ = 0x89;
            /// <summary>標準サイズ</summary>
            internal const byte NSZ = 0x8A;
            /// <summary>指定サイズ</summary>
            internal const byte SZX = 0x8B;
            /// <summary>色指定<br />パラメーター 1byte または 2bytes</summary>
            internal const byte COL = 0x90;
            /// <summary>フラッシング制御<br />パラメーター 1byte</summary>
            internal const byte FLC = 0x91;
            /// <summary>コンシール制御<br />パラメーター 1byte または 2bytes</summary>
            internal const byte CDC = 0x92;
            /// <summary>パターン極性<br />パラメーター 1byte</summary>
            internal const byte POL = 0x93;
            /// <summary>書込みモード変更<br />パラメーター 1byte</summary>
            internal const byte WMM = 0x94;
            /// <summary>マクロ指定<br />パラメーター 不定長</summary>
            internal const byte MACRO = 0x95;
            /// <summary>囲み制御<br />パラメーター 1byte</summary>
            internal const byte HLC = 0x97;
            /// <summary>文字繰り返し<br />パラメーター 1byte</summary>
            internal const byte RPC = 0x98;
            /// <summary>アンダーライン開始およびモザイク分離終了</summary>
            internal const byte SPL = 0x99;
            /// <summary>アンダーライン開始およびモザイク分離開始</summary>
            internal const byte STL = 0x9A;
            /// <summary>コントロールシーケンスイントロデューサ<br />パラメーター 不定長</summary>
            internal const byte CSI = 0x9B;
            /// <summary>時間制御<br />パラメーター 2bytes</summary>
            internal const byte TIME = 0x9D;
        }
    }
}
