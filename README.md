# SimpleBlockPG

ドラッグ＆ドロップによるシンプルなビジュアルプログラミングを実装するための Unity ライブラリです。

ブロックの組み立て・保存・復元・取り消しまでを提供します。実行処理系（組んだブロックを解釈して動かす部分）は含みません。利用側で実装します。

## 導入

Package Manager の **Add package from git URL** に以下を入力します。

```
https://github.com/nitou-kanazawa/lib-unity-SimpleBlockPG.git#upm
```

`upm` ブランチは、`main` のテストが通るたびに CI がパッケージ部分だけを切り出して更新します。

### 依存ライブラリ

**UniRx が必要です。** Git URL 依存は `package.json` に書けない（UPM はレジストリ上のパッケージしか解決しない）ため、**利用側の `Packages/manifest.json` に手で追加してください。**

```json
{
  "dependencies": {
    "com.neuecc.unirx": "https://github.com/neuecc/UniRx.git?path=Assets/Plugins/UniRx/Scripts"
  }
}
```

`com.unity.ugui`（TextMeshPro を含む）と `com.unity.inputsystem` は `package.json` で宣言済みのため、自動で解決されます。

### 動作環境

Unity 6000.0 以降。開発は 6000.4.8f1 で行っています。

## デモ

ブラウザで触れます。`main` の最新が自動で反映されます。

**https://nitou-kanazawa.github.io/lib-unity-SimpleBlockPG/**

## サンプル

Package Manager の **Samples** から `Demo` をインポートすると、シーンが `Assets/Samples/` へ入ります。

| シーン | 内容 |
| --- | --- |
| `00-Hub` | デモの入口。各シーンへ移動し、戻ってこられます |
| `06-Playground` | ブロックの組み立て・保存・取り消し・折り畳み。5 種類のテーマで見た目を切り替えられます |
| `07-InputBlocks` | 入力値とブロック固有データが保存・復元されることを確かめられます |

`00-Hub` からの移動には、シーンが Build Settings に登録されている必要があります。**インポート直後は登録されていないため、Hub のカードも「Back to Hub」ボタンも出ません。** 各シーンを個別に開けば単体で動きます。使い方は [00-Hub の README](Packages/com.nitou.blockpg/Samples/Demo/00-Hub/README.md) を参照してください。

## できること

- ブロックのドラッグ＆ドロップによる組み立て（接続・切断・入れ子）
- スクリプトからの操作（生成・削除・複製・接続・切断）
- XML への保存と復元。**深さによらず 1 フレームで組み上がります**
- 取り消し／やり直し
- スコープブロックの折り畳み
- 入力欄（文字列・数値・選択肢）と、ブロック固有データの保存
- クリック／右クリック（スマホでは長押し）／ホバーのイベント。スタンドアロンとモバイルの差はライブラリ側で吸収します
- 見た目のカスタマイズ（配色・角丸・影・枠線）

### 含まないもの

- **実行処理系** — 組んだブロックを解釈して動かす部分は利用側で実装します。拡張点として `I_BPG_Instruction` / `BPG_BlockInstruction` を用意しています
- **ルート階層でのブロック連結** — ルートブロック同士は繋がりません。連結には親セクションが要ります

## ドキュメント

`Packages/com.nitou.blockpg/Documentation~/` にあります。

| ファイル | 内容 |
| --- | --- |
| [Layout.md](Packages/com.nitou.blockpg/Documentation~/Layout.md) | レイアウトの方針。uGUI の `LayoutGroup` に依存しない理由と、その実装 |
| [Serialization.md](Packages/com.nitou.blockpg/Documentation~/Serialization.md) | 何が保存されるか。入力値・固有データの扱い |
| [Scripting.md](Packages/com.nitou.blockpg/Documentation~/Scripting.md) | スクリプトからのブロック操作 |

## 開発

このリポジトリは Unity プロジェクトを兼ねており、`Packages/com.nitou.blockpg/` が埋め込みパッケージとして解決されます。

- テストはパッケージ内にあります。**埋め込みパッケージのテストは Test Runner が自動で拾う**ため、`testables` への登録は不要です（登録が要るのは、レジストリや Git URL 経由で入れたパッケージのテストを走らせたい場合）
- `Assets/_Development/` は開発用のシーンで、パッケージには含まれません

### サンプルの扱い

`Packages/com.nitou.blockpg/Samples/Demo/` を**そのまま Unity から開いて編集できます**。チルダを付けていないため、開発プロジェクトでは通常のフォルダとして取り込まれます。

配布時にだけ `Samples~` へ変える必要があるので、これは CI（`.github/workflows/publish-upm.yml`）が行います。

```
main へ push
  └ Test が成功
      └ Publish UPM branch
          ├ git subtree split -P Packages/com.nitou.blockpg -b upm
          ├ Samples -> Samples~ にリネーム（Samples.meta は削除）
          └ upm ブランチへ force push
```

チルダを付けないまま配ると、**利用者のプロジェクトへサンプルが常にインポートされてしまいます**。逆に開発リポジトリでチルダを付けると、Unity が取り込まないため編集できません。この 2 つを両立させるためのリネームです。

### WebGL デモの公開

`main` へ push すると、デモを WebGL でビルドして GitHub Pages へ公開します。起動シーンは `00-Hub` です。

```
main へ push
  └ Deploy WebGL demo
      ├ game-ci/unity-builder で WebGL ビルド
      └ GitHub Pages へデプロイ
```

**WebGL の圧縮設定はエディタから変更しないでください。** Unity の既定（Brotli）は
サーバ側でヘッダを返す前提のため、GitHub Pages では動きません。詳細は
[webgl-demo.md](Docs/webgl-demo.md) を参照してください。

## ライセンス

MIT License

サンプルには M PLUS 1p Regular（SIL Open Font License 1.1）を同梱しています。ライセンス全文はフォントと同じ場所（`Packages/com.nitou.blockpg/Samples/Demo/Resources/Fonts/OFL.txt`）にあります。**組み込みフォントに日本語のグリフが無く、WebGL では OS フォントへフォールバックできない**ため同梱しています。詳細は [webgl-demo.md](Docs/webgl-demo.md) を参照してください。
