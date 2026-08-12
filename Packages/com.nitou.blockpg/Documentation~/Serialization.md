# 保存と復元

## 何が保存されるか

| 対象 | 保存先 | 備考 |
| --- | --- | --- |
| 識別 ID | `<id>` | インスタンスごとに一意。実行をまたいでも復元できる |
| プレハブ名 | `<name>` | 復元時に `Resources/BlockPG/` から引く鍵になる |
| 位置 | `<localPosition>` | ルートブロックのみ意味を持つ |
| 折り畳み状態 | `<isCollapsed>` | セクションごと |
| 入力値 | `<inputs>` | ヘッダーの入力要素の値。**順序で対応づける** |
| ブロック固有データ | `<customData>` | 持たない場合は要素ごと省略 |
| 子ブロック | `<childBlocks>` | 再帰 |

## 復元はプレハブからの生成

復元は保存された名前でプレハブを読み、そこから生成する。**実行時に付けたコンポーネントは復元後のブロックには存在しない。**

入力要素も固有データの受け手も、プレハブ側に居る必要がある。テストが `Block [TestInput]` という専用プレハブを持っているのはこのため。

復元は同期的に完結する。深さ N の木でも 1 フレームで組み上がるため、呼び出し側は戻り値をそのまま使える。

## 入力値

ヘッダーに入力要素を置くと、値が自動的に保存対象になる。

```csharp
public sealed class MyInput : BPG_BlockSectionHeader_InputBase {
    protected override void ApplyToView(string value) { /* 表示へ反映 */ }
}
```

同梱している実装は 3 つ。

| クラス | 対応ウィジェット | 値 |
| --- | --- | --- |
| `BPG_BlockSectionHeader_TextInput` | `TMP_InputField` | 入力文字列 |
| `BPG_BlockSectionHeader_NumberInput` | `TMP_InputField` | 数値の文字列表現 |
| `BPG_BlockSectionHeader_Dropdown` | `TMP_Dropdown` | 選択肢の**文字列** |

### 決めごと

**値は文字列で持つ。** 保存形式（`SerializableInput`）が文字列 1 本のため。数値も選択肢もそれぞれの型で解釈したうえで文字列にする。

**ドロップダウンはインデックスではなく文字列を保存する。** インデックスで保存すると、選択肢の並びを変えただけで既存データの意味が変わる。

**数値はインバリアントカルチャで文字列化する。** 小数点が `,` になる環境で保存したデータが他の環境で読めなくなるのを防ぐため。

**入力欄の変更は `onEndEdit` で受ける。** 1 打鍵ごとに正規化すると、`-` や `1.` のような入力途中の状態がその場で丸められて打てなくなる。

**ウィジェットが未設定でも値は保持できる。** 保存と復元は見た目が無くても成立すべきで、テストもその形で検証している。

### 対応づけ

入力は**順序**で対応づける。プレハブ側の入力構成が保存時から変わっていると個数が食い違うため、処理できる範囲だけ復元して警告を出す。セクション数の食い違いと同じ扱い。

## ブロック固有データ

入力値では表せない情報（参照先の ID、色、独自の設定など）は `I_BPG_BlockCustomData` で持たせる。

```csharp
public sealed class MyBlockData : MonoBehaviour, I_BPG_BlockCustomData {
    public string SaveCustomData() => JsonUtility.ToJson(_state);
    public void LoadCustomData(string data) => _state = JsonUtility.FromJson<State>(data);
}
```

ブロックと同じ `GameObject` に付ける。文字列で受け渡すのは保存形式を XML に閉じ込めるためで、中身の形式は実装側が決めてよい。

保存データに固有データがあるのに受け手が居ない場合は警告する。プレハブ構成の変更で黙って失われる事故を見つけるため。

## Undo との関係

`BPG_UndoHistory` はスナップショット方式で、保存・復元と同じ経路を通る。**保存されないものは取り消しでも戻らない。**

## 後方互換

要素が欠けていても復元は継続する。`isCollapsed` / `inputs` / `customData` はいずれも後から足したもので、無ければ既定値として扱う。
