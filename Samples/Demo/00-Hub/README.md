# 00-Hub

デモシーンの入口です。ここから各デモへ移動し、戻ってこられます。

ブラウザでも試せます: https://nitou-kanazawa.github.io/lib-unity-SimpleBlockPG/

## 構成について

UI はシーンに置かず、`DemoHub.Start()` から実行時に構築しています。他のデモシーンと同じ方針です。

シーンに置いてあるのは以下だけです。

```
Main Camera
EventSystem
DemoHub          … 一覧UIを構築し、選ばれたシーンへ遷移する
```

一覧の中身は [DemoSceneCatalog.cs](../Scripts/DemoSceneCatalog.cs) にあります。デモを増やすときはここへ 1 行足してください。

## 戻るボタンについて

各デモシーンの右下に出る「Back to Hub」は、**デモ側のコードには一切手を入れずに**差し込んでいます。

[DemoNavigation.cs](../Scripts/DemoNavigation.cs) が `SceneManager.sceneLoaded` を購読し、Hub 以外のシーンが読み込まれたときに専用の Canvas を重ねます。デモ側がレイアウトを変えても影響を受けません。

配置はテーマバー右側の空き領域に合わせてあります。

## Package Manager からインポートした場合

**戻るボタンは出ません。一覧のカードも並びません。**

Samples としてインポートしたシーンは利用者の Build Settings に登録されないため、そのままシーン遷移を試みると例外になります。`DemoSceneCatalog.IsInBuild()` で登録済みか確認し、無ければ機能ごと無効にしています。

Hub を使いたい場合は、**File → Build Profiles**（旧 Build Settings）へ以下を登録してください。

```
Assets/Samples/SimpleBlockPG/<version>/Demo/00-Hub/00-Hub.unity
Assets/Samples/SimpleBlockPG/<version>/Demo/06-Playground/06-Playground.unity
Assets/Samples/SimpleBlockPG/<version>/Demo/07-InputBlocks/07-InputBlocks.unity
```
