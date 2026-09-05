# WebGL デモの公開

`main` の最新を、ブラウザで触って動作確認できるようにするための仕組み。

公開先: **https://nitou-kanazawa.github.io/lib-unity-SimpleBlockPG/**

## 全体像

```
main へ push
  └ Deploy WebGL demo (.github/workflows/deploy-webgl-pages.yml)
      ├ game-ci/unity-builder で WebGL ビルド
      │   └ buildMethod: nitou.BlockPG.BuildTools.WebGLBuilder.Build
      └ GitHub Pages へデプロイ
```

公開されるのは `00-Hub` / `06-Playground` / `07-InputBlocks` の 3 シーンで、
起動シーンは `00-Hub` です。ビルド対象は
[`Assets/_Development/Editor/WebGLBuilder.cs`](../Assets/_Development/Editor/WebGLBuilder.cs)
の `SCENES` に直接書いてあります（**先頭が起動シーンになる**）。

デモを増やしたら、`SCENES` と `DemoSceneCatalog.Scenes` の両方へ追加してください。
`SCENES` にだけ足しても Hub に並ばず、`DemoSceneCatalog` にだけ足しても
ビルド設定に無いためカードが出ません。

`EditorBuildSettings` を参照していないのは、あれが開発中の作業対象を入れる場所で
頻繁に書き換わるためです。公開されるものが手元の作業状態に引きずられないよう、
デモの内容はコード側で固定しています。

## 有効化に必要な作業

**どちらもリポジトリ管理者しか実行できません。**

### 1. GitHub Pages を有効にする

**Settings → Pages → Build and deployment → Source** を **GitHub Actions** にします。

`Deploy from a branch` のままだと、ワークフローのデプロイステップが失敗します。

### 2. Unity ライセンスの Secrets

`test.yml` と同じ `UNITY_LICENSE` / `UNITY_EMAIL` / `UNITY_PASSWORD` を使います。
テストの CI が動いているなら追加作業はありません。

手順は [CI_SETUP.md](../.github/CI_SETUP.md) を参照してください。

## 圧縮設定について

**Unity の既定（Brotli）のままでは GitHub Pages 上で動きません。**

Unity の WebGL ビルドは圧縮済みのファイルを出力し、サーバが
`Content-Encoding: br` を返すことを前提にしています。GitHub Pages は
カスタムヘッダを設定できないため、ブラウザが解凍できずロードに失敗します。

そこで次の構成にしてあります。

| 設定 | 値 | 理由 |
| --- | --- | --- |
| Compression Format | Gzip | Unity 同梱の JS デコンプレッサが解ける |
| Decompression Fallback | ON | サーバのヘッダに頼らず、ランタイム側で解凍する |

`ProjectSettings.asset` に保存してあるうえ、`WebGLBuilder` がビルド時にも設定します。
エディタ上で戻されても成果物が壊れないようにするためです。

**この設定はエディタから変更しないでください。** 変えるとデモが真っ白になります。

## ローカルでビルドする

エディタのメニューから **Tools → BlockPG → Build WebGL Demo** を実行します。
出力先は `build/WebGL/`（`.gitignore` 対象）です。

`file://` で直接開いても動きません。ローカル確認にはサーバが要ります。

```bash
python3 -m http.server 8080 --directory build/WebGL
```

## 制約と注意

- **PR ごとのプレビューは作れません。** GitHub Pages は 1 リポジトリ 1 サイトのため、
  公開できるのは `main` の最新だけです。PR ごとの URL が要るようになったら
  Cloudflare Pages や Netlify へ移すことになります（そちらはヘッダを設定できるので
  Brotli もそのまま使えます）
- **ビルドは 20〜40 分かかります。** Unity の WebGL 用 Docker イメージが約 7GB あり、
  展開だけで時間を取ります。`Library` のキャッシュが効けば短くなります
- **ディスクの空きが不足しがちです。** ワークフローの `Free up disk space` ステップは
  そのための処理で、外すと `No space left on device` で落ちます
- 保存先は `Application.persistentDataPath` で、WebGL ではブラウザの IndexedDB に
  なります。ブラウザやシークレットウィンドウを変えると保存内容は共有されません

## トラブルシューティング

| 症状 | 原因 |
| --- | --- |
| 画面が真っ白／ロードバーで止まる | 圧縮設定が Brotli に戻っている。ブラウザのコンソールを確認する |
| デプロイステップが 404 で失敗する | Pages の Source が `GitHub Actions` になっていない |
| ライセンス認証で失敗する | Secrets が未登録、またはライセンスが失効している |
| `index.html が見つかりません` | ビルドは通ったが成果物が空。ビルドログでシーンの読み込みを確認する |
| `No space left on device` | ランナーのディスク不足。`Free up disk space` の削除対象を増やす |
