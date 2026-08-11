using UnityEngine;

namespace nitou.BlockPG.DragDrop {
    using nitou.BlockPG.Interface;

    public sealed class BPG_SpotOuterArea : BPG_Spot {

        public override Vector2 DropPosition => RectTransform.position;


        private void Awake() {
            GatherComponents();
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private void GatherComponents() {
            Block = GetComponentInParent<I_BPG_Block>();
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnValidate() {
            RectTransform.pivot = new Vector2(0f, 1f);
        }
#endif
    }


    public static partial class BPG_SpotConnect_Extensions {

        public static bool InsertNextTo(this I_BPG_Block block, BPG_SpotOuterArea spotOuterArea) {
            // place to the same parent as spot block
            var parentSection = spotOuterArea.Block.ParentSection;
            var index = spotOuterArea.Block.GetIndexInSection();
            if (index < 0) return false;

            parentSection.Body.Append(block, index + 1);

            //var spotBlockParent = spotOuterArea.Block.RectTransform.parent;
            //int spotBlockIndex = spotOuterArea.Block.RectTransform.GetSiblingIndex();

            //spotBlockParent.
            //block.RectTransform.SetParent(spotBlockParent);
            //block.RectTransform.SetSiblingIndex(spotBlockIndex + 1);

            return true;
        }
    }
}
