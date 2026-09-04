using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// ブロック階層に残った uGUI の<see cref="LayoutGroup"/>を検出する．
    /// </summary>
    /// <remarks>
    /// ブロックの配置はライブラリ側が行うため、同じ場所に LayoutGroup が付いていると
    /// **二重に効いて配置が競合する**．同梱プレハブからは撤去済みだが、
    /// 利用者が独自に作ったプレハブには残っている可能性がある．
    ///
    /// [NOTE] 検出対象は「ライブラリが配置を決める役割」を持つオブジェクトだけに絞る．
    ///        ヘッダーアイテムの内側などで利用者が LayoutGroup を使うのは正当なため、
    ///        階層まるごとを対象にすると誤検出になる．
    /// </remarks>
    public static class BPG_LayoutGroupGuard {

        /// <summary>
        /// 指定オブジェクトが、ライブラリが配置を決める役割を持つかどうか．
        /// </summary>
        public static bool IsLayoutOwner(GameObject target) {
            if (target == null)
                return false;

            return target.GetComponent<I_BPG_BlockLayout>() != null
                || target.GetComponent<I_BPG_BlockSection>() != null
                || target.GetComponent<I_BPG_BlockSectionHeader>() != null
                || target.GetComponent<I_BPG_BlockSectionBody>() != null;
        }

        /// <summary>
        /// 競合する<see cref="LayoutGroup"/>を階層から集める．
        /// </summary>
        public static List<LayoutGroup> Collect(GameObject root) {
            var results = new List<LayoutGroup>();
            if (root == null)
                return results;

            foreach (var group in root.GetComponentsInChildren<LayoutGroup>(includeInactive: true)) {
                if (IsLayoutOwner(group.gameObject)) {
                    results.Add(group);
                }
            }
            return results;
        }

        /// <summary>
        /// 競合する<see cref="LayoutGroup"/>があるかどうか．
        /// </summary>
        public static bool HasConflict(GameObject root) {
            return Collect(root).Count > 0;
        }

        /// <summary>
        /// 競合していれば警告を出す．
        /// </summary>
        /// <remarks>
        /// [NOTE] 呼び出し元のオブジェクト自身だけを見る．階層を辿ると、
        ///        1つの競合について各階層から重複して警告が出るため．
        /// </remarks>
        public static void WarnIfConflicting(Component owner) {
            if (owner == null)
                return;

            var group = owner.GetComponent<LayoutGroup>();
            if (group == null)
                return;

            Debug.LogWarning(
                $"'{owner.gameObject.name}' has {group.GetType().Name}. " +
                $"Block layout is managed by the library, so it conflicts. Remove the layout group. " +
                $"(Tools > BlockPG > Find Conflicting Layout Groups)",
                owner.gameObject);
        }

#if UNITY_EDITOR
        // ※同じプレハブから何個生成しても警告は1回に留める
        private static readonly HashSet<int> _warnedRoots = new();

        /// <summary>
        /// プレハブに競合があれば警告する．（※プレハブごとに1回だけ）
        /// </summary>
        /// <remarks>
        /// [NOTE] OnValidate はプレハブを開く／取り込むまで走らないため、
        ///        一度も触っていないプレハブの競合は生成するまで気づけない．
        ///        生成の経路でも見ておく．
        /// </remarks>
        public static void WarnOnceForPrefab(GameObject prefabRoot) {
            if (prefabRoot == null)
                return;

            if (!_warnedRoots.Add(prefabRoot.GetInstanceID()))
                return;

            var conflicts = Collect(prefabRoot);
            if (conflicts.Count == 0)
                return;

            Debug.LogWarning(
                $"Block prefab '{prefabRoot.name}' has {conflicts.Count} conflicting layout group(s). " +
                $"Block layout is managed by the library, so they conflict. " +
                $"(Tools > BlockPG > Find Conflicting Layout Groups)",
                prefabRoot);
        }
#endif
    }
}
