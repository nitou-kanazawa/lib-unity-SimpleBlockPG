using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace nitou.BlockPG.EditorScripts {
    using nitou.BlockPG.Blocks;

    /// <summary>
    /// ブロック階層に残った uGUI の LayoutGroup を、プロジェクト全体から探して取り除く．
    /// </summary>
    /// <remarks>
    /// 同梱プレハブからは撤去済みだが、利用者が独自に作ったプレハブには残っている可能性がある．
    /// 残っていると、ライブラリ側の配置と二重に効いて表示が崩れる．
    ///
    /// [NOTE] 開いているシーンも対象にする．シーンに直接置かれたブロック
    ///        （ゴーストブロックなど）は、プレハブ走査だけでは拾えないため．
    /// </remarks>
    public static class BPG_LayoutGroupMigration {

        private const string MENU_ROOT = "Tools/BlockPG/";


        /// ----------------------------------------------------------------------------
        // Menu

        [MenuItem(MENU_ROOT + "Find Conflicting Layout Groups")]
        private static void Find() {
            var prefabHits = CollectFromPrefabs();
            var sceneHits = CollectFromOpenScenes();

            if (prefabHits.Count == 0 && sceneHits.Count == 0) {
                Debug.Log("[BlockPG] No conflicting layout groups were found.");
                return;
            }

            Debug.LogWarning(BuildReport(prefabHits, sceneHits));
        }

        [MenuItem(MENU_ROOT + "Remove Conflicting Layout Groups")]
        private static void Remove() {
            var prefabHits = CollectFromPrefabs();
            var sceneHits = CollectFromOpenScenes();

            int total = prefabHits.Sum(h => h.count) + sceneHits.Sum(h => h.count);
            if (total == 0) {
                EditorUtility.DisplayDialog("BlockPG",
                    "競合する LayoutGroup は見つかりませんでした。", "OK");
                return;
            }

            // ※アセットを書き換えるため、対象を見せてから確認する
            bool proceed = EditorUtility.DisplayDialog("BlockPG",
                $"{total} 個の LayoutGroup を取り除きます。\n\n" +
                $"プレハブ: {prefabHits.Count} 件\n" +
                $"開いているシーン: {sceneHits.Count} 件\n\n" +
                "対象の一覧はコンソールに出力済みです。",
                "取り除く", "やめる");

            Debug.LogWarning(BuildReport(prefabHits, sceneHits));
            if (!proceed)
                return;

            int removed = RemoveFromPrefabs(prefabHits) + RemoveFromOpenScenes(sceneHits);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BlockPG] Removed {removed} conflicting layout group(s).");
        }


        /// ----------------------------------------------------------------------------
        // 検出

        private struct Hit {
            public string path;         // プレハブのパス、またはシーンのパス
            public string[] objects;    // 対象オブジェクトの名前
            public int count;
        }

        private static List<Hit> CollectFromPrefabs() {
            var hits = new List<Hit>();

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab")) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                    continue;

                var conflicts = BPG_LayoutGroupGuard.Collect(asset);
                if (conflicts.Count == 0)
                    continue;

                hits.Add(new Hit {
                    path = path,
                    objects = conflicts.Select(c => $"{c.gameObject.name} ({c.GetType().Name})").ToArray(),
                    count = conflicts.Count,
                });
            }
            return hits;
        }

        private static List<Hit> CollectFromOpenScenes() {
            var hits = new List<Hit>();

            for (int i = 0; i < EditorSceneManager.sceneCount; i++) {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                var conflicts = new List<UnityEngine.UI.LayoutGroup>();
                foreach (var root in scene.GetRootGameObjects()) {
                    conflicts.AddRange(BPG_LayoutGroupGuard.Collect(root));
                }
                if (conflicts.Count == 0)
                    continue;

                hits.Add(new Hit {
                    path = scene.path,
                    objects = conflicts.Select(c => $"{c.gameObject.name} ({c.GetType().Name})").ToArray(),
                    count = conflicts.Count,
                });
            }
            return hits;
        }

        private static string BuildReport(List<Hit> prefabHits, List<Hit> sceneHits) {
            var builder = new StringBuilder();
            builder.AppendLine("[BlockPG] Conflicting layout groups found.");
            builder.AppendLine("ブロックの配置はライブラリ側が行うため、同じ場所の LayoutGroup は二重に効きます。");

            Append(builder, "Prefabs", prefabHits);
            Append(builder, "Open scenes", sceneHits);
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, string heading, List<Hit> hits) {
            if (hits.Count == 0)
                return;

            builder.AppendLine().AppendLine($"--- {heading} ---");
            foreach (var hit in hits) {
                builder.AppendLine($"{hit.path}");
                foreach (var name in hit.objects) {
                    builder.AppendLine($"    {name}");
                }
            }
        }


        /// ----------------------------------------------------------------------------
        // 除去

        private static int RemoveFromPrefabs(List<Hit> hits) {
            int removed = 0;

            foreach (var hit in hits) {
                // [NOTE] LoadPrefabContents で編集用の実体を開く．アセットを直接触ると
                //        ネストしたプレハブの取り扱いで壊れることがある．
                var root = PrefabUtility.LoadPrefabContents(hit.path);
                try {
                    foreach (var group in BPG_LayoutGroupGuard.Collect(root)) {
                        Object.DestroyImmediate(group, allowDestroyingAssets: true);
                        removed++;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, hit.path);
                } finally {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            return removed;
        }

        private static int RemoveFromOpenScenes(List<Hit> hits) {
            int removed = 0;

            for (int i = 0; i < EditorSceneManager.sceneCount; i++) {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !hits.Any(h => h.path == scene.path))
                    continue;

                foreach (var root in scene.GetRootGameObjects()) {
                    foreach (var group in BPG_LayoutGroupGuard.Collect(root)) {
                        Object.DestroyImmediate(group);
                        removed++;
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);
            }

            // ※シーンの保存は利用者に委ねる（開いている編集内容を勝手に確定させない）
            if (removed > 0) {
                Debug.Log("[BlockPG] Scene changes are not saved automatically. Save the scene to keep them.");
            }
            return removed;
        }
    }
}
