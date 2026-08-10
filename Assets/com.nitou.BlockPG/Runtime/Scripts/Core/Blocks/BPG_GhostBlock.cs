using UnityEngine;

namespace nitou.BlockPG.Blocks{

    // [NOTE] 

    /// <summary>
    /// Dummy block instance for visual effect.
    /// In order to calculate size of block section accuately, it is necessary to implements <see cref="Interface.I_BPG_Block"/>.
    /// </summary>
    public class BPG_GhostBlock : BPG_BlockBase{

        public override BlockType Type => BlockType.None;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        // [NOTE] Awake() を直接宣言すると基底の Awake() が呼ばれなくなるため、OnInitialize() を override する．
        protected override void OnInitialize() {
            GatherComponents();

            // [NOTE] block which parent section is null was can not connectable.
            //BPG_InputEventBus.OnPrimaryKeyUpEnd.Subscribe(_ => GatherParentSection()).AddTo(this);
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// Show ghost block.
        /// </summary>
        public void Show(Transform parent, Vector3 localScale, int siblingIndex = 0) {
            transform.SetParent(parent);
            transform.SetSiblingIndex(siblingIndex);
            transform.localScale = localScale;
            AdjustTransform();

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide ghost block.
        /// </summary>
        public void Hide() {
            AdjustTransform();

            gameObject.SetActive(false);
        }
    }
}
