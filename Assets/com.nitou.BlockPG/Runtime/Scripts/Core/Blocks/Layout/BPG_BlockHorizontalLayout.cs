using System.Collections.Generic;
using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;

    [DisallowMultipleComponent]
    public sealed class BPG_BlockHorizontalLayout : BPG_ComponentBase {

        [SerializeField] Color _blockColor = Color.white;
        [SerializeField] bool _highlight = false;

        private readonly List<I_BPG_BlockSection> _sections = new();


        /// ----------------------------------------------------------------------------
        // Public Method

        public void GatherSections() {
            _sections.Clear();
            foreach (Transform child in transform) {
                if (child.TryGetComponent<I_BPG_BlockSection>(out var section)) {
                    _sections.Add(section);
                }
            }
        }

        /// <summary>
        /// Updates the layout of the block. Used to correctly resize the blocks after adding child and operation blocks
        /// </summary>
        public void UpdateLayout() {
            //RectTransform.sizeDelta = Size;
            _sections.ForEach(section => section.UpdateLayout());
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR


        void OnValidate() {
            GatherSections();
        }
#endif
    }
}
