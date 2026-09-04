# 06-Playground

SimpleBlockPG でできることを一通り試せるデモシーンです。

`Packages/com.nitou.blockpg/Samples/Demo/06-Playground/06-Playground.unity` を開いて再生してください。

ブラウザでも試せます: https://nitou-kanazawa.github.io/lib-unity-SimpleBlockPG/

## できること

| 操作 | 内容 |
| --- | --- |
| 左のパレット | ブロックを 4 種類（Entry / Normal / Scope / MultiScope）追加する |
| ブロックをドラッグ | 他のブロックへ重ねると連結する。入れ子も可能 |
| ワークスペースの外へドラッグ | ブロックを削除する |
| **ブロックを右クリック / 長押し** | **メニューを開く（複製 / 削除 / 折り畳み）** |
| Save | 組み立てた内容をファイルへ保存する |
| Load | 保存した内容を復元する |
| Clear | ワークスペースを空にする |
| Fold | 折り畳めるセクションをまとめて開閉する |
| Undo / Redo | 直前の操作を取り消す / やり直す |
| 下部のテーマボタン | 見た目を 5 パターンで切り替える |

### 入力について

**右クリックと長押しは同じイベント（`OnSecondaryAction`）に束ねられています。**
デモ側はプラットフォームを判定しておらず、購読は 1 本だけです。

```csharp
BPG_BlockEventBus.OnSecondaryAction.Subscribe(OpenContextMenu);
```

スタンドアロンでは右クリック、スマホ・タブレットでは長押しで同じメニューが開きます。

保存先は `Application.persistentDataPath/BlockPG/demo-workspace.xml` です。

## テーマについて

見た目のカスタマイズ性を示すため、5 つのプリセットを用意しています。

| テーマ | 方向性 |
| --- | --- |
| Scratch | 王道のブロックプログラミング風。原色と柔らかい影 |
| Midnight | 暗色背景にネオン系の発色。影を使わず枠線で見せる |
| Pastel | 低彩度でやわらかい配色。角丸を大きく取る |
| Paper | 紙とインク。影を使わず太い輪郭で構成する |
| Terminal | 端末風。角を落として蛍光色を並べる |

テーマの定義は [DemoTheme.cs](../Scripts/DemoTheme.cs) にまとまっています。
色・角丸・枠線・影をパラメータで持っているだけなので、プリセットを増やすのは
`DemoTheme.CreateAll()` に 1 つ足すだけです。

### 何を変えていて、何を変えていないか

- **変えているもの**: ブロックの配色、ラベル枠の色と角丸、影の有無・向き・色、
  背景色、パネル・ボタンの配色と角丸、枠線、文字色
- **変えていないもの**: ブロック本体のスプライト

ブロックのスプライトは連結部の凹凸を含む 9 スライス画像のため、差し替えると
形状が壊れます。テーマでは色と装飾のみを扱っています。

角丸スプライトは外部アセットを持ち込まずに済むよう、
[DemoUIFactory.cs](../Scripts/DemoUIFactory.cs) で手続き的に生成しています。

## 構成について

UI はシーンに置かず、`BlockPGDemo.Start()` から実行時に構築しています。
テーマごとの差分をコードで一望できるようにするためです。

シーンに置いてあるのは以下だけです。

```
Canvas
├── Workspace        … BPG_ProgrammingEnv（ブロックの配置先 兼 ドロップ判定範囲）
├── GhostBlock       … 接続先の予告表示
└── DraggingLayer    … ドラッグ中のブロックの一時的な配置先
DraggingSystem
BlockPGDemo
EventSystem
```

`Workspace` を画面全体ではなくパレット・ツールバーを除いた範囲にしてあるのは、
その外へドロップしたブロックが破棄される挙動をそのまま「削除」として使うためです。
