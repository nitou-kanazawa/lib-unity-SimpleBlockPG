using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// <c>Resources</c>からブロックのプレハブを引く既定のカタログ．
    /// </summary>
    public sealed class BPG_ResourcesBlockCatalog : I_BPG_BlockCatalog {

        /// <summary>既定の読み込み先．</summary>
        public const string DEFAULT_FOLDER_PATH = "BlockPG";

        private readonly string _folderPath;

        /// <summary>
        /// 読み込み先のフォルダ．
        /// </summary>
        public string FolderPath => _folderPath;


        /// ----------------------------------------------------------------------------
        // Public Method

        public BPG_ResourcesBlockCatalog(string folderPath = DEFAULT_FOLDER_PATH) {
            _folderPath = string.IsNullOrEmpty(folderPath) ? DEFAULT_FOLDER_PATH : folderPath;
        }

        /// <summary>
        /// ブロック名に対応するプレハブを取得する．（※見つからない場合はnull）
        /// </summary>
        /// <remarks>
        /// [NOTE] Resources.Load&lt;TBlock&gt;() の戻り値を直接キャストすると常に null になるため、
        ///        GameObject を取得してからコンポーネントを引く．
        /// </remarks>
        public I_BPG_Block GetPrefab(string blockName) {
            if (string.IsNullOrEmpty(blockName)) {
                Debug.LogWarning("Block name must not be null or empty.");
                return null;
            }

            var path = $"{_folderPath}/{blockName}";
            var prefabObj = Resources.Load<GameObject>(path);
            if (prefabObj == null) {
                Debug.LogWarning($"Block prefab is not found. (path: {path})");
                return null;
            }

            if (!prefabObj.TryGetComponent<I_BPG_Block>(out var prefab)) {
                Debug.LogWarning($"Loaded prefab does not have a block component. (path: {path})");
                return null;
            }

            return prefab;
        }
    }
}
