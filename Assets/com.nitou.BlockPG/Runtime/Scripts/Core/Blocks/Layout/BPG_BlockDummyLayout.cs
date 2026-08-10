using UnityEngine;

namespace nitou.BlockPG.Blocks{
    using nitou.BlockPG.Interface;
    using System.Collections.Generic;

    [DisallowMultipleComponent]
    public class BPG_BlockDummyLayout : BPG_ComponentBase, I_BPG_BlockLayout {

        private readonly List<I_BPG_BlockSection> _sections = new();

        /// <summary>
        /// Block visible color.
        /// </summary>
        public Color Color {get; set;}

        /// <summary>
        /// Returns the size of the whole block. Headers and Bodies with child blocks are counted on.
        /// </summary>
        public Vector2 Size => Vector2.zero;

        /// <summary>
        /// Child sections.
        /// </summary>
        public IReadOnlyList<I_BPG_BlockSection> Sections => _sections;


        /// ----------------------------------------------------------------------------
        // Public Method

        public void GatherSections() {
            _sections.Clear();
            foreach (Transform chiled in transform) {
                if (chiled.TryGetComponent<I_BPG_BlockSection>(out var section)) {
                    _sections.Add(section);
                }
            }
        }

        /// <summary>
        /// レイアウトの再計算を予約する．
        /// [NOTE] サイズが常に zero のダミー実装のため、自身の更新予約は不要．
        ///        祖先のサイズには影響しうるので、伝播のみ行う．
        /// </summary>
        public void SetLayoutDirty() {
            var parent = transform.parent;
            if (parent != null) {
                parent.GetComponentInParent<I_BPG_BlockLayout>()?.SetLayoutDirty();
            }
        }

        /// <summary>
        /// Updates the layout of the block. Used to correctly resize the blocks after adding child and operation blocks
        /// </summary>
        public void UpdateLayout() {
            RectTransform.sizeDelta = Size;
            _sections.ForEach(section => section.UpdateLayout());
        }
    }
}
