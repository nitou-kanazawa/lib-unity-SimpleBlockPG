# CI のセットアップ

EditMode / PlayMode のテストを PR ごとに実行するワークフローです。
**有効化するには 2 つの作業が必要で、どちらもリポジトリ管理者しか実行できません。**

1. ワークフローファイル `.github/workflows/test.yml` の設置（下記 0 章）
2. Unity ライセンスの Secrets 登録（下記 1 章）

## 0. ワークフローファイルを設置する

ワークフローの追加には GitHub トークンの `workflow` スコープが必要なため、
このファイルはリポジトリに含まれていません。次のいずれかで設置してください。

**A. ローカルブランチを push する（推奨）**

`ci/github-actions` ブランチにワークフローを含むコミットが用意してあります。

```bash
git push -u origin ci/github-actions
```

push 後に PR を作成してマージしてください。

**B. 手で作成する**

`.github/workflows/test.yml` として以下を保存してください。

```yaml
name: Test

on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
  workflow_dispatch:

# 同じブランチで新しい push があれば古い実行を打ち切る
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  test:
    name: ${{ matrix.testMode }}
    runs-on: ubuntu-latest
    permissions:
      contents: read
      checks: write

    strategy:
      fail-fast: false
      matrix:
        testMode:
          - editmode
          - playmode

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      # Library をキャッシュしないと毎回インポートからやり直しになる
      - uses: actions/cache@v4
        with:
          path: Library
          key: Library-${{ matrix.testMode }}-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: |
            Library-${{ matrix.testMode }}-
            Library-

      - uses: game-ci/unity-test-runner@v4
        id: tests
        env:
          # [NOTE] Personal ライセンスの場合はこの3つ、Pro の場合は UNITY_SERIAL を使う．
          #        未登録だとこのステップでライセンス認証に失敗する．
          UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
        with:
          # ProjectSettings/ProjectVersion.txt から自動で解決する
          unityVersion: auto
          testMode: ${{ matrix.testMode }}
          artifactsPath: ${{ matrix.testMode }}-artifacts
          githubToken: ${{ secrets.GITHUB_TOKEN }}
          checkName: ${{ matrix.testMode }} results
          coverageOptions: generateAdditionalMetrics;generateHtmlReport;dontClear

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: Test results (${{ matrix.testMode }})
          path: ${{ steps.tests.outputs.artifactsPath }}
          retention-days: 14

      - uses: actions/upload-artifact@v4
        if: always()
        with:
          name: Coverage results (${{ matrix.testMode }})
          path: ${{ steps.tests.outputs.coveragePath }}
          retention-days: 14
```

## 1. Secrets を登録する

Personal ライセンス（無料）の場合は 3 つ登録します。

| Secret | 内容 |
| --- | --- |
| `UNITY_LICENSE` | ライセンスファイル `Unity_v2021.x.ulf` の**中身をそのまま**貼り付け |
| `UNITY_EMAIL` | Unity アカウントのメールアドレス |
| `UNITY_PASSWORD` | Unity アカウントのパスワード |

`UNITY_LICENSE` の取得手順は GameCI の
[Activation ガイド](https://game.ci/docs/github/activation/)に従ってください。
`.alf` を生成して Unity のライセンスサイトに通し、返ってきた `.ulf` の中身を使います。

Pro / Plus ライセンスの場合は `UNITY_LICENSE` の代わりに `UNITY_SERIAL` を登録し、
`test.yml` の `env` を差し替えてください。

登録先は **Settings → Secrets and variables → Actions → New repository secret** です。

## 2. マージの必須条件にする

Secrets 登録後、一度 CI を成功させてからブランチ保護を設定します。
チェック名は実行実績がないと候補に出てこないためです。

**Settings → Branches → Add branch ruleset**（または Branch protection rules）で
`main` に対して以下を設定します。

- Require a pull request before merging
- Require status checks to pass before merging
  - `editmode` を必須に追加
  - `playmode` を必須に追加

## 3. Unity バージョンについて

`unityVersion: auto` により `ProjectSettings/ProjectVersion.txt` から自動で解決されます。
現在は `6000.4.8f1` で、対応する Docker イメージ `unityci/editor:ubuntu-6000.4.8f1-base-3`
の存在を確認済みです。

Unity をアップグレードした際は、対応するイメージが
[Docker Hub](https://hub.docker.com/r/unityci/editor/tags) に存在するか確認してください。
GameCI は新しいエディタバージョンを自動でビルドしていますが、公開までに時間差があります。

## ローカルでの実行

Unity CLI から実行できます。**Editor はプロジェクトロックで競合するため、先に閉じてください。**

```bash
unity test --mode EditMode --output editmode-results.xml --timeout 600
```

```bash
unity test --mode PlayMode --output playmode-results.xml --timeout 600
```
