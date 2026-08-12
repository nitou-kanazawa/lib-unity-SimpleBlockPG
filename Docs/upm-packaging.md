# UPM パッケージ配布の構成

このリポジトリで採用した構成と、その理由。**ライブラリ用テンプレートへ横展する前提でまとめる。**

## 全体像

```
リポジトリ = Unity プロジェクト
├── Assets/
│   └── _Development/          開発用シーン（配布しない）
├── Packages/
│   └── com.nitou.blockpg/     ← 埋め込みパッケージ。配布する実体
│       ├── package.json
│       ├── Runtime/ Editor/ Tests/
│       ├── Documentation~/
│       └── Samples/           ← チルダ無し。開発中は普通に開ける
├── Docs/                      設計資料（配布しない）
└── .github/workflows/
    ├── test.yml               main / PR でテスト
    └── publish-upm.yml        テスト成功後に upm ブランチを更新
```

利用者の導入は `https://github.com/<owner>/<repo>.git#upm`。

## 選択肢と、選んだ理由

### 1. パッケージの置き場所

| 案 | 例 | 備考 |
| --- | --- | --- |
| リポジトリのルート | 多くの小規模ライブラリ | 開発用の Unity プロジェクトを別に用意する必要がある |
| `Assets/` の中 | [UniTask](https://github.com/Cysharp/UniTask), [UniRx](https://github.com/neuecc/UniRx), [NaughtyAttributes](https://github.com/dbrizov/NaughtyAttributes) | `Samples~` が機能しない |
| **`Packages/` の中** | [Unity 公式 InputSystem](https://github.com/Unity-Technologies/InputSystem) | **採用**。埋め込みパッケージとして解決される |

`Packages/` を選ぶ理由は **`Samples~` と Package Manager が機能すること**。`Assets/` 配下は UPM パッケージとして認識されないため、サンプルの仕組みが使えない。

UniTask / UniRx が `Assets/` なのは、どちらもサンプルを持たないため。

### 2. 配布方法

| 案 | 備考 |
| --- | --- |
| `?path=Packages/com.xxx` | 追加作業ゼロで動く。ただしサンプルのチルダ問題が残る |
| **`#upm` ブランチ（subtree split）** | **採用**。CI が切り出すぶん、配布物を加工できる |

`?path=` でも配布自体は成立する。**CI を挟む価値は「配布時にだけ加工できる」点**にある。

## Samples のチルダ問題

これが構成上いちばん厄介な点。

| 置き方 | 開発プロジェクトで編集 | 利用者への影響 |
| --- | --- | --- |
| `Samples`（チルダ無し） | ○ | **常にインポートされる**（不要なアセットが混入） |
| `Samples~`（チルダ付き） | **✗ Unity が取り込まない** | ○ インポートは任意 |

Unity 公式ドキュメントは前者を指示している。

> Don't append a trailing tilde (`~`). During the export process, Unity will rename the `Samples` folder to `Samples~` automatically.

ただし**自動リネームが働くのは export（レジストリ公開）を経る場合だけ**。Git URL で直接配ると export 工程が無いため、チルダ無しのまま利用者へ届いてしまう。

### 解決

**リポジトリでは `Samples`、配布時に CI が `Samples~` へリネームする。**

```yaml
git subtree split -P "$PKG_ROOT" -b "$UPM_BRANCH"
git checkout "$UPM_BRANCH"
git mv Samples "Samples~"
git rm -q Samples.meta        # 隠しフォルダに .meta は不要（孤立 meta になる）
git commit -m "chore: rename Samples to Samples~ for distribution"
git push -f origin "$UPM_BRANCH"
```

これで開発中は普通に開けて、利用者には任意インポートで届く。

## 踏んだ落とし穴

### Samples 内の `.meta` は消してはいけない

サンプルはインポート時に `Assets/` へコピーされる。シーンからスクリプトへの参照は **GUID** で保存されているため、`.meta` が無いと GUID が振り直され、**インポートしたシーンのコンポーネントが軒並み Missing になる**。

配布物から消してよいのは、隠しフォルダ自身の `Samples.meta` だけ。

検証は「インポート → シーンを開く → Missing コンポーネント数が 0」で行う。

### `testables` は埋め込みパッケージには不要

`Packages/` 配下のパッケージのテストは Test Runner が自動で拾う。`testables` が要るのは、**レジストリや Git URL 経由で入れた**パッケージのテストを走らせたい場合。

Unity 公式の InputSystem も `testables` を持たない。

ただし**テストが認識されているかは件数で確認すること**。認識されていなくても CI は「0 件成功」で緑になり、気づけない。

```
gh run view <run-id> --log | grep '</test-run><test-run'
```

### Git URL 依存は `package.json` に書けない

UPM の `dependencies` はレジストリ上のパッケージしか解決しない。UniRx や UniTask のような Git URL 依存は**利用者が自分の `manifest.json` に手で書く**必要がある。

→ 依存は減らせるだけ減らし、残るものは README に手順を明記する。

このリポジトリでは UniTask を標準の `Task` に置き換えて外した（使用箇所が非同期セーブ／ロードだけだったため）。UniRx は公開 API が `IObservable<T>` を返すため残している。

### `Assets/` → `Packages/` の GUID 参照

`Assets` 側の asmdef から `Packages` 側の asmdef を **GUID 参照すると解決できなかった**。名前参照（`"Nitou.BlockPG"`）へ変更して解消。

### 移動中に出るモーダル

パッケージ移動でシーンが行き先を失うと、Unity が「Save Scene」「Scene(s) Have Been Modified」を出す。**モーダルはメインスレッドを専有するため、この間 MCP のコマンドは一切通らない**（ハングと区別がつかない）。

いずれも保存せずに閉じてよい。保存すると迷子のシーンファイルが生まれる。

## テンプレートへ持っていくもの

- `Packages/<package-name>/` にパッケージを置くリポジトリ構成
- `.github/workflows/test.yml`（PR と main でテスト）
- `.github/workflows/publish-upm.yml`（テスト成功後に subtree split ＋ Samples リネーム）
- `.gitignore` に `/[Aa]ssets/Samples/`（インポートした複製を追跡しない）
- README の雛形（導入 URL、Git URL 依存の追加手順、サンプル、開発時の構成）
