# SimpleBlockPG


## 概要

　ドラッグ＆ドロップによるシンプルなビジュアルプログラミングを実装するためのライブラリです．


## リポジトリ構成

　現在、新旧2つの実装が並存しています．

| ディレクトリ | 位置づけ | 名前空間 | asmdef |
|---|---|---|---|
| `Assets/com.nitou.BlockPG` | **現行実装**．uBlock が完成するまでの実働版 | `nitou.BlockPG.*` | `Nitou.BlockPG` |
| `Assets/uBlock` | **本命**．設計を見直した書き直し版（実装途中） | `Nitou.uBlock` | `Nitou.uBlock` |

　両者はコードを共有しておらず、互いに参照もしていません．

### 設計の違い

　`com.nitou.BlockPG` は、ブロックの木構造を Unity の Transform 階層そのもので表現しています．
親子関係は `GetComponentInParent()` や `transform.parent` を辿って取得するため、
データ構造とシーン階層を切り離せません．

　`uBlock` では `Core/Logic` と `Core/View` を分離しています．
`Core/Logic` は MonoBehaviour に依存しない純粋な C# のツリー構造（`ITreeNode<TNode>` ほか）で、
`Core/View` が表示のみを担当します．
これによりロジック単体でのテストが可能になり、シリアライズやレイアウト更新も
構造の変化から直接導けるようになります．

### 今後の方針

- 旧実装（`com.nitou.BlockPG`）は**削除しません**．uBlock が実用に足りるまで現行実装として維持します．
- `uBlock` はある程度実装が進んだ時点で**別リポジトリへ分離**する予定です．
  - このため、`uBlock` から `com.nitou.BlockPG` への依存を持ち込まないでください．


## 依存ライブラリ

- UniRx  https://github.com/neuecc/UniRx
- UniTask  https://github.com/Cysharp/UniTask
- Input System（`com.unity.inputsystem`）
- uGUI（`com.unity.ugui`）


## 開発環境

　Unity 6000.0.30f1


## コーディング上の注意

- **ソースコードは UTF-8 (BOM付き) で保存してください．**
  BOM を外すと日本語ロケールの IDE が Shift-JIS として保存し直し、コメントが文字化けします．
  （過去に複数回発生しています）
