using UnityEngine;

namespace nitou.BlockPG.DragDrop {
    using nitou.BlockPG.Interface;

    public sealed class BPG_SpotBlockBody : BPG_Spot{

        public override Vector2 DropPosition => RectTransform.position;

        public I_BPG_BlockSectionBody SectionBody { get; private set; }


        private void Awake() {
            GatherComponents();
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private void GatherComponents() {
            Block = GetComponentInParent<I_BPG_Block>();
            SectionBody = GetComponent<I_BPG_BlockSectionBody>();
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnValidate() {
            RectTransform.pivot = new Vector2(0f, 1f);
        }
#endif
    }


    public static partial class BPG_SpotConnect_Extensions {

        public static void ConnectTo(this I_BPG_Block block, BPG_SpotBlockBody spotBlockBody) {
            spotBlockBody.SectionBody.AppendFirst(block);
        }
    }
}
