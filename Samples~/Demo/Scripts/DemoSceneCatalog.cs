using System;
using System.IO;
using UnityEngine.SceneManagement;

namespace nitou.BlockPG.Demo {

    /// <summary>
    /// Hubから辿れるデモシーンの一覧．
    /// </summary>
    public static class DemoSceneCatalog {

        /// <summary>
        /// 入口となるシーン名．
        /// </summary>
        public const string HubSceneName = "00-Hub";

        /// <summary>
        /// 一覧に並べる1件分．
        /// </summary>
        public readonly struct Entry {

            public readonly string SceneName;
            public readonly string Title;
            public readonly string Description;

            public Entry(string sceneName, string title, string description) {
                SceneName = sceneName;
                Title = title;
                Description = description;
            }
        }

        /// <summary>
        /// 並べる順にデモシーンを保持する．
        /// </summary>
        public static readonly Entry[] Scenes = {
            new Entry("06-Playground", "Playground",
                "ブロックの組み立て・保存・取り消し・折り畳み．テーマ切り替えも試せる"),
            new Entry("07-InputBlocks", "Input Blocks",
                "入力値とブロック固有データが保存・復元されることを確かめる"),
        };


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 指定シーンがビルド設定に含まれるか判定する．
        /// </summary>
        // [NOTE] Samples としてインポートした利用者の環境では、シーンはビルド設定に入らない．
        //        含まれないシーンを読み込もうとすると例外になるため、遷移の前に必ず確認する．
        public static bool IsInBuild(string sceneName) {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName, StringComparison.Ordinal)) {
                    return true;
                }
            }
            return false;
        }
    }
}
