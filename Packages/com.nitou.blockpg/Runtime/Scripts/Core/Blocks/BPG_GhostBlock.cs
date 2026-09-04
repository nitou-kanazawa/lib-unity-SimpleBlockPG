using UnityEngine;

namespace nitou.BlockPG.Blocks{
    using nitou.BlockPG.Interface;

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

            MarkParentLayoutDirty();
        }

        /// <summary>
        /// Hide ghost block.
        /// </summary>
        public void Hide() {
            AdjustTransform();

            // ※非アクティブ化すると親を辿れなくなるため、先に更新を予約する
            MarkParentLayoutDirty();

            gameObject.SetActive(false);
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// 配置先ブロックのレイアウト更新を予約する．
        /// [NOTE] 表示／非表示の切り替えは再ペアレントを伴わないため、
        ///        セクション側の OnTransformChildrenChanged では検知できない．
        /// </summary>
        private void MarkParentLayoutDirty() {
            var parent = transform.parent;
            if (parent != null) {
                parent.GetComponentInParent<I_BPG_BlockLayout>()?.SetLayoutDirty();
            }
        }
    }
}
