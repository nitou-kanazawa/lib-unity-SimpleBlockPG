using UnityEngine;

namespace nitou.BlockPG.Environments{
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Blocks;
    using nitou.BlockPG.DragDrop;

    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(BPG_SpotProgrammingEnv))]
    public class BPG_ProgrammingEnv : BPG_ComponentBase, I_BPG_ProgrammingEnv {

        private CanvasGroup _canvasGroup;

        // [NOTE] インターフェース型はシリアライズできないため、UnityEngine.Object として受けて
        //        取得時にキャストする．入れ違いは OnValidate で気づけるようにする．
        [Header("Overrides (optional)")]
        [SerializeField] Object _blockCatalog;
        [SerializeField] Object _blockFactory;

        private I_BPG_BlockCatalog _catalogOverride;
        private I_BPG_BlockFactory _factoryOverride;

        private static readonly I_BPG_BlockCatalog DefaultCatalog = new BPG_ResourcesBlockCatalog();
        private static readonly I_BPG_BlockFactory DefaultFactory = new BPG_DefaultBlockFactory();


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>
        /// ブロック名からプレハブを引くカタログ．（※未設定なら Resources から引く）
        /// </summary>
        public I_BPG_BlockCatalog BlockCatalog {
            get => _catalogOverride ?? (_blockCatalog as I_BPG_BlockCatalog) ?? DefaultCatalog;
        }

        /// <summary>
        /// プレハブからブロックの実体を作るファクトリ．（※未設定なら Instantiate する）
        /// </summary>
        public I_BPG_BlockFactory BlockFactory {
            get => _factoryOverride ?? (_blockFactory as I_BPG_BlockFactory) ?? DefaultFactory;
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// カタログを差し替える．（※nullで既定へ戻す）
        /// </summary>
        public void SetBlockCatalog(I_BPG_BlockCatalog catalog) {
            _catalogOverride = catalog;
        }

        /// <summary>
        /// ファクトリを差し替える．（※nullで既定へ戻す）
        /// </summary>
        public void SetBlockFactory(I_BPG_BlockFactory factory) {
            _factoryOverride = factory;
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnValidate() {
            if (_blockCatalog != null && !(_blockCatalog is I_BPG_BlockCatalog)) {
                Debug.LogWarning($"{_blockCatalog.name} does not implement {nameof(I_BPG_BlockCatalog)}.", this);
                _blockCatalog = null;
            }
            if (_blockFactory != null && !(_blockFactory is I_BPG_BlockFactory)) {
                Debug.LogWarning($"{_blockFactory.name} does not implement {nameof(I_BPG_BlockFactory)}.", this);
                _blockFactory = null;
            }
        }
#endif
    }
}
