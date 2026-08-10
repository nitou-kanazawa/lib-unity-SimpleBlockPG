using UnityEngine;

namespace nitou.BlockPG{
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Blocks;

    public static class BPG_BlockUtils {

        // [NOTE] Resources 直下からの相対パス．
        //        実体は Runtime/Resources/BlockPG/ にあるため、末尾に "Blocks" は付かない．
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
            
            // create instance
            var block = MonoBehaviour.Instantiate<TBlock>(blockPrefab);

            // setup param
            block.name = blockPrefab.name;
            block.transform.localPosition = Vector3.zero;
            block.transform.localEulerAngles = Vector3.zero;

            programmingEnv.Append(block);
            return block;
        }

    }
}
