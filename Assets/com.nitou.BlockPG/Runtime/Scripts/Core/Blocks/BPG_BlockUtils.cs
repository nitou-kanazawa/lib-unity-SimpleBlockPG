using System.Linq;
using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Events;

    /// <summary>
    /// ブロックの生成と破棄．
    /// </summary>
    public static class BPG_BlockUtils {

        // [NOTE] Resources 直下の実配置は "BlockPG/" のため、"BlockPG/Blocks" では常にロードに失敗していた．
        private static readonly string folderPath = "BlockPG";

        public static TBlock LoadBlockPrefab<TBlock>(string prefabName)
            where TBlock : BPG_BlockBase {

            // [NOTE] Resources.Load<GameObject>() の戻り値を TBlock へ直接キャストすると常に null になるため、
            //        GameObject を取得してからコンポーネントを引く．
            var prefabObj = Resources.Load<GameObject>($"{folderPath}/{prefabName}");
            if (prefabObj == null) {
                Debug.LogWarning($"Block prefab is not found. (path: {folderPath}/{prefabName})");
                return null;
            }

            if (!prefabObj.TryGetComponent<TBlock>(out var prefab)) {
                Debug.LogWarning($"Loaded prefab does not have {typeof(TBlock).Name}. (path: {folderPath}/{prefabName})");
                return null;
            }

            return prefab;
        }

        public static BPG_BlockBase LoadBlockPrefab(string prefabName) {
            return LoadBlockPrefab<BPG_BlockBase>(prefabName);
        }


        public static TBlock CreateBlock<TBlock>(TBlock blockPrefab, I_BPG_ProgrammingEnv programmingEnv)
            where TBlock : BPG_BlockBase {

            if (blockPrefab == null)
                throw new System.ArgumentNullException(nameof(blockPrefab));
            if (programmingEnv == null)
                throw new System.ArgumentNullException(nameof(programmingEnv));

            // create instance
            // [NOTE] 配置先を指定して生成する．シーンルートに生成してから付け替えると、
            //        Append() が worldPositionStays: true で再ペアレントする際に
            //        Canvas のスケールを打ち消す localScale が入ってしまう
            //        （scaleFactor が 1 でない環境ではブロックが 1/scaleFactor 倍に拡大される）．
            var block = MonoBehaviour.Instantiate<TBlock>(blockPrefab, programmingEnv.RectTransform);

            // setup param
            block.name = blockPrefab.name;
            block.transform.localPosition = Vector3.zero;
            block.transform.localEulerAngles = Vector3.zero;
            block.transform.localScale = Vector3.one;

            programmingEnv.Append(block);
            return block;
        }

        /// <summary>
        /// ブロックを子孫ごと破棄する．
        /// </summary>
        public static void RemoveBlock(I_BPG_Block block) {
            if (block is null)
                return;

            // 子のブロックから逆順で破棄イベントを発火
            foreach (var childBlock in block.GetAllChaildBlocks(containSelf: true).Reverse<I_BPG_Block>()) {
                BPG_BlockEventBus.PublishDestroyEvent(childBlock);
            }

            // 破棄
            Object.Destroy(block.RectTransform.gameObject);
        }
    }
}
