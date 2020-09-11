# AribB24.DotNet

**"ARIB STD-B24 8-bit Character Code" decoder for .NET Standard**

**「ARIB STD-B24 8単位符号」デコーダー**

「BDAV形式で録画されたテレビ番組のタイトルや番組説明を取り出す」ことを目的としたライブラリです。現在のところ字幕関係は未実装です。

# Install

```
PM> Install-Package AribB24.DotNet
```

もしくは

```
dotnet add package AribB24.DotNet
```

# Usage

```csharp
using System.Text;
using AribB24.DotNet;

B24Decoder encoder = new B24Decoder();
byte[] bytes = File.ReadAllBytes("input"); // デコードしたいバイト列
string str = encoder.GetString(bytes);

// ReadOnlySpan<byte> も引数に取れます
Span<byte> span = bytes.AsSpan().Slice(15, 10);
str = encoder.GetString(span);
```

（名前が HogeEncoding でないのは、`System.Text.Encoding` を実装していないためです）

# ARIB STD-B24 8単位符号 について

ARIB STD-B24 8単位符号 は、日本におけるデジタルテレビ放送・FM文字多重放送で用いられる文字符号化形式です。

ISO/IEC 2022 に独自の文字集合を割り当てた方式で、次のような特徴があります。

- ひらがな、カタカナを1バイトで表現できる
- 第四水準の漢字が表現できる
- 道路交通情報（VICS）用の記号（`⛍`や`🅿`など）、ラテ欄でよく見る記号（`🈞`や`🈡`など）が豊富
  - これらの記号は Unicode 5.2 に収録された

参考資料:<br>
[標準規格の入手について（STD-B24）](https://www.arib.or.jp/kikaku/kikaku_hoso/std-b24.html)……以前は日本語版も無償でした<br>
[ISO/IEC 2022 ‐ 通信用語の基礎知識](https://www.wdic.org/w/WDIC/ISO/IEC%202022)<br>
[ARIB STD-B24用iconv(gconv)モジュール - PukiWiki](http://www.minkycute.homeip.net/pukiwiki/index.php?ARIB%20STD-B24%E7%94%A8iconv%28gconv%29%E3%83%A2%E3%82%B8%E3%83%A5%E3%83%BC%E3%83%AB)

# 符号変換方式

この実装では、規格書（『ARIB STD-B24 6.1版』）に従いつつ、.NET で日本語の文字コード・文字集合として標準的に用いられる CP932 (Windows-31J) との整合性を考慮しながら、ARIB STD-B24 8単位符号 を Unicode に変換します。

なお、Unicode から ARIB STD-B24 8単位符号 への変換については考慮しません。

##  制御符号「文字サイズ」の取り扱い

Unicode では、JIS X 0208/0213 の英数カタカナ（いわゆる全角文字）と、JIS X 0201 の英数カタカナ（いわゆる半角文字）を区別するため、異なるコードポイントが割り当てられています。

一方、ARIB STD-B24 8単位符号 には、文字の大きさをコントロールするための制御符号 `[MSZ]` (08/9 (0x89)) と `[NSZ]` (08/10 (0x8A)) が存在します。

規格書では次のように定義されています。

> `[MSZ]` ... 中型サイズ ... 文字の大きさを中型とすること。<br>
> `[NSZ]` ... 標準サイズ ... 文字の大きさを標準とすること。

……よくわからないので、実際に ARIB STD-B24 8単位符号 でエンコードされたバイト列や、それを扱う家電製品の動作を調べると、次のようなルールが見えてきます。

  1. `[MSZ]` の後に続く文字は、すべて半角文字として表示する。
  2. `[NSZ]` の後に続く文字は、すべて全角文字として表示する。
  3. 初期状態では `[NSZ]` が指示されているものとする。

このような調査結果をもとに、この実装では次の通り符号変換を行います。

  1. `[MSZ]` の後に続く文字は、ASCII / JIS X 0201 由来のコードポイントにマッピングする。
  2. `[NSZ]` の後に続く文字は、JIS X 0208/0213 由来のコードポイントにマッピングする。

一例として、`0E 1B 7C A2 A4 89 A6 A8 41 42 8A 43 44` というバイト列をデコードした例を次に示します。なお、`[LS1]` (00/14 (0x0E)) は G1 を GL に指示する制御符号、`[ESC] |` (01/11 07/12 (0x1B 0x7C)) は G3 を GR に指示する制御符号列です。

<table>
  <tr>
    <th rowspan="2">バイト列</th>
    <td>0E</td><td>1B</td><td>7C</td><td>A2</td><td>A4</td><td>89</td><td>A6</td><td>A8</td><td>41</td><td>42</td><td>8A</td><td>43</td><td>44</td>
  </tr>
  <tr>
    <td>LS1</td><td>ESC</td><td>|</td><td></td><td></td><td>MSZ</td><td></td><td></td><td></td><td></td><td>NSZ</td><td></td><td>
  </tr>
  <tr>
    <th>GL</th>
    <td>G0</td><td colspan="12">G1（英数集合）</td>
  </tr>
  <tr>
    <th>GR</th>
    <td colspan="3">G2</td><td colspan="10">G3（片仮名集合）</td>
  </tr>
  <tr>
    <th>サイズ</th>
    <td colspan="6">全角</td><td colspan="5">半角</td><td colspan="2">全角</td>
  </tr>
  <tr>
    <th>文字</th>
    <td colspan="3"></td><td>ア</td><td>イ</td><td></td><td>ｳ</td><td>ｴ</td><td>A</td><td>B</td><td></td><td>Ｃ</td><td>Ｄ</td>
  </tr>
</table>

結果は `アイｳｴABＣＤ` となります。`ｳｴAB` は半角で、他は全角です。

## YEN SIGN / REVERSE SOLIDUS

英数集合の円記号 (05/12 (0x5C)) 、および 漢字系集合・JIS 互換漢字 1 面集合の円記号 (1区79点) は、次のコードポイントに変換します。

<table>
  <tr>
    <th>[MSZ] が指示されているとき</th>
    <td><code>¥</code> YEN SIGN (U+00A5)</td>
  </tr>
  <tr>
    <th>[NSZ] が指示されているとき</th>
    <td><code>￥</code> FULLWIDTH YEN SIGN (U+FFE5)</td>
  </tr>
</table>

## TILDE / OVER LINE

英数集合の OVER LINE (07/14 (0x7E)) は、次のコードポイントに変換します。

<table>
  <tr>
    <th>[MSZ] が指示されているとき</th>
    <td><code>~</code> TILDE (U+007E)</td>
  </tr>
  <tr>
    <th>[NSZ] が指示されているとき</th>
    <td><code>～</code> FULLWIDTH TILDE (U+FF5E)</td>
  </tr>
</table>

なお規格書では、第一編第2部付録規定E『UCS と 8単位符号、EUC-JP、シフトJIS の変換ならびに、拡張文字・DRCS との対応』において

> 8 単位符号との変換においては、JIS X 0201 にて規定される OVER LINE (符号値0x7E) は TILDE (符号値0x007E) に変換することとする。

と記述されています。

## 無視する文字

字幕制御に関する制御符号（表示位置の指定など）は無視します。また、モザイク集合、外字符号集合、マクロ符号集合に属する文字もすべて無視します。
