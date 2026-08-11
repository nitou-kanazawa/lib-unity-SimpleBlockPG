# ブロックのレイアウト

## 方針

**ブロックのレイアウトは uGUI の LayoutGroup に依存しない。** サイズも位置もライブラリ側で決める。

### なぜ自前で持つか

もともとはサイズをライブラリが決め、位置決めを LayoutGroup が行う二重管理だった。この構造には 2 つの問題があった。

**1. 真実が 2 つある**

親セクションは子の `Layout.Size`（計算値）から高さを確保する一方、実際の配置は LayoutGroup が子の `sizeDelta`（実サイズ）で行う。両者がずれると、確保した高さと描画内容が食い違って隙間になる。

過去の不具合はいずれもこの構造に起因していた。

- レイアウト更新が入れ子のブロックへ伝播しない
- サイズ確定の順序が逆で、親の確保領域と描画がずれる

**2. コストが釣り合わない**

全 LayoutGroup が `childControl = false` で構成されていた。つまり LayoutGroup は縦積みの位置決めしかしておらず、数十行で書ける仕事のために uGUI の再構築機構一式を引き連れていた。

22 ブロックのネスト構造での実測では、uGUI の再構築は独自計算の 13 倍のコストだった。

### 判断の記録

- 入れ子の LayoutGroup は「厳禁」と言われることがあるが、**計測した限り増え方は線形**で、指数的な悪化は起きていなかった。`childControl = false` だったため、親子でサイズを問い合わせ合う多段パスが発生していなかったことによる
- したがって撤去の主目的は**性能ではなく、真実を 1 つにすること**。性能改善は副次的な効果

## 責務分担

| 対象 | 決めるもの |
| --- | --- |
| `BPG_BlockVerticalLayout` | ブロック全体のサイズ、セクションの積み上げ |
| `BPG_BlockSection` | セクションのサイズ、ヘッダーとボディの積み上げ |
| `BPG_BlockSectionHeader` | ヘッダーのサイズ |
| `BPG_BlockSectionBody` | ボディのサイズ、子ブロックのレイアウト更新 |
| `BPG_LayoutUtils` | 積み上げの計算 |

## 更新の流れ

```
構成の変化（子の追加・削除・並び替え）
  └ OnTransformChildrenChanged
      └ SetLayoutDirty()  ── 祖先ブロックへ伝播
          └ ルートブロックの LateUpdate
              └ UpdateLayout()  ── 部分木全体を1回の再帰で更新
```

- 変化が無いフレームでは何も走らない（dirty フラグ）
- 子ブロック自身の `LateUpdate` は、親を持つ場合に早期リターンする。**部分木の更新はルートからの 1 回の再帰で完結させる**

## 順序の規則

**必ず「子が先、自分が後」。** これを守らないと、自分のサイズだけが 1 回前の値のまま残る。

```csharp
public void UpdateLayout() {
    _sections.ForEach(section => section.UpdateLayout());   // 子を確定
    RectTransform.sizeDelta = Size;                         // 自分を確定
    BPG_LayoutUtils.StackChildrenVertically(transform);     // 位置を決める
}
```

`Size` はセクションの現在のサイズを合計する**計算プロパティ**であり、参照した時点の値を返す。順序を逆にすると古い値を拾う。

dirty フラグは更新後にクリアされるため、1 回の走査でずれると次に構成が変わるまで解消されない。

この規則は `BlockSizeConsistencyTest` が「全ブロックで `sizeDelta == Layout.Size`」という不変条件で縛っている。

## 配置ルール

`BPG_LayoutUtils.StackChildrenVertically()` は直下の子を上から順に縦へ積む。

```
anchorMin = anchorMax = (0, 1)
x = 幅   * pivot.x
y = -積み上げ位置 - 高さ * (1 - pivot.y)
```

この式は推測ではなく、LayoutGroup が実際に設定していた値を実行時に採取して導いた。撤去の前後で座標が完全に一致することを確認している。

サイズは変更しないため、**呼び出し前に各子のサイズが確定している必要がある**。

## レイアウトから除外する

装飾（選択枠・バッジ・エラーアイコンなど）をブロックに重ねる場合、積み上げの対象から外す。

```csharp
// 単純に除外する
overlay.AddComponent<BPG_LayoutIgnore>();
```

```csharp
// 状態に応じて切り替える
public sealed class SelectionFrame : MonoBehaviour, I_BPG_LayoutIgnore {
    public bool IgnoreLayout => true;
}
```

- 非アクティブなオブジェクトも対象外になる
- 無効化されたコンポーネントの指定は尊重しない

> uGUI の `ILayoutIgnorer` / `LayoutElement` は**使わない**。それらを使うと「除外したければ uGUI のレイアウトコンポーネントを付けろ」という要求になり、LayoutGroup から脱却する目的と矛盾する。加えて `LayoutElement` の `preferredHeight` などは一切参照されないため、効くように見えて効かない設定項目を晒すことになる。

## 配置パラメータ

余白・間隔・寄せは各コンポーネントが serialize して持つ。既定値は撤去した LayoutGroup の設定と同じ。

| 階層 | 方向 | padding (L/R/T/B) | spacing | 寄せ |
| --- | --- | --- | --- | --- |
| ブロック直下 | 縦 | 0 / 0 / 0 / 0 | 0 | 左上 |
| セクション | 縦 | 0 / 0 / 0 / 0 | 0 | 左上 |
| ヘッダー | 横 | 15 / 0 / 10 / 20 | 15 | 左・縦中央 |
| ボディ | 縦 | 20 / 0 / -10 / 0 | -10 | 左上 |

ボディの間隔と上余白が負なのは、**ブロック同士を接続部の凹凸ぶん食い込ませる**ため。

## 積み上げ方向

ブロックは縦横どちらの方向にも構成できる。方向はブロックのレイアウトコンポーネントが決め、**セクション・ヘッダーとボディ・子ブロックのすべてがその方向に従う。**

| コンポーネント | 方向 | 想定 |
| --- | --- | --- |
| `BPG_BlockVerticalLayout` | 上から下 | 文を並べるブロック |
| `BPG_BlockHorizontalLayout` | 左から右 | 式を組み立てるブロック |

共通の挙動（更新の予約と伝播、セクションの収集、再帰更新）は `BPG_BlockLayoutBase` にまとまっており、派生クラスは `Axis` を返すだけでよい。

サイズの合成は方向に応じて入れ替わる。

| | 積み上げ方向 | 直交方向 |
| --- | --- | --- |
| ブロック | セクションの合計 | セクションの最大値 |
| セクション | ヘッダー + ボディ | ヘッダーの値 |
| ボディ | 子ブロックの合計（間隔込み） | セクションの値 |

軸の扱いは `BlockLayoutAxis` の `Along` / `Across` / `ToSize` に閉じている。x と y を直接書かず、これらを通すこと。

現時点で横レイアウトを使うプレハブは同梱していない。利用する場合は、ブロックのルートに `BPG_BlockHorizontalLayout` を付ける。

## 現状と今後

**ブロック階層に LayoutGroup は存在しない。** `BlockLayoutPerformanceTest` が 0 個であることを検証している。

`BPG_BlockSectionBody.UpdateSelfSize()` の長さ計算には、間隔と同じ意味の数値がまだ直書きされている。配置側とは独立しているため、整理は別途行う。

## 計測

`BlockLayoutPerformanceTest` が以下を継続的に検証している。

- レイアウト更新がブロック数に対して線形であること
- ブロックのルートが uGUI のレイアウトルートでないこと
- ブロックあたりの LayoutGroup 数が上限内であること
- 構成が変化しないフレームで更新が走らないこと

CI は共有ランナーで実行時間がぶれるため、**絶対時間はしきい値に使わない**。実測値はログに出して推移を追う。
