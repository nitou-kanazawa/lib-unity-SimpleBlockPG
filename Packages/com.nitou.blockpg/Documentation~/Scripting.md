# スクリプトからのブロック操作

ドラッグ操作を介さず、コードからブロックを組み立てる。

## 一覧

| 操作 | API |
| --- | --- |
| 生成 | `BPG_BlockUtils.LoadBlockPrefab(name, env)` → `BPG_BlockUtils.CreateBlock(prefab, env)` |
| 削除 | `BPG_BlockUtils.RemoveBlock(block)` |
| 複製 | `BPG_BlockSerializer.Duplicate(block, env)` |
| 接続（末尾） | `body.AppendLast(block)` / `AppendFirst` / `Append(block, index)` |
| 接続（相対） | `block.InsertAfter(target)` / `block.InsertBefore(target)` |
| 接続（ブロック指定） | `block.AppendTo(parentBlock, sectionIndex, siblingIndex)` |
| 切断 | `block.Detach()` |
| 保存 / 復元 | `BPG_BlockStorage.Save/Load` — [Serialization.md](Serialization.md) |
| 取り消し | `BPG_UndoHistory` |

```csharp
var prefab = BPG_BlockUtils.LoadBlockPrefab("Block [Scope]", env);
var scope  = BPG_BlockUtils.CreateBlock(prefab, env);

var child = BPG_BlockUtils.CreateBlock(normalPrefab, env);
scope.GetFirstSection().Body.AppendLast(child);

var second = BPG_BlockUtils.CreateBlock(normalPrefab, env);
second.InsertAfter(child);

second.Detach();   // ルートへ戻す
```

## 決めごと

### 切断は画面上の位置を維持する

切り離しは再ペアレントなので、そのままだと配置先の原点へ飛ぶ。`Detach()` はワールド座標を控えて戻す。

### 同じスタック内の移動は 1 つずれる

`InsertBefore` / `InsertAfter` を**同じセクション内**で使うと、自分が抜けたぶん後ろの要素が 1 つ詰まる。インデックスをそのまま使うと 1 つずれた位置に入るため、内部で補正している。

```
[A, B, C] で A.InsertAfter(C)
  補正なし → SetSiblingIndex(3) → [B, C, A] ではなく範囲外/ずれ
  補正あり → [B, C, A]
```

`BlockScriptingTest` が同一スタック内の前後移動を押さえている。

### 生成後の整理はライブラリが行う

名前・座標・環境への接続・生成イベントの発行は `BPG_BlockUtils.CreateBlock` の責務。ファクトリを差し替えても変わらない（[Layout.md](Layout.md) の「解決と生成」も参照）。

**名前は復元時にプレハブを引く鍵**なので、独自にリネームしないこと。

## できないこと

### ルートブロック同士は連結できない

接続には親セクションが要る。`InsertAfter` / `InsertBefore` の対象がルートブロックの場合は `false` を返す。

そのため**スタックの分割（Scratch でいう「途中から下をまとめて引き抜く」）は成立しない。** 途中のブロックを `Detach()` すると、後続は元のスタックに残る。

連れて出るには、ルート階層にもスタックを置ける器（コンテナ）の概念が要る。現状のモデルには無いため、対応する場合は別途設計が要る。
