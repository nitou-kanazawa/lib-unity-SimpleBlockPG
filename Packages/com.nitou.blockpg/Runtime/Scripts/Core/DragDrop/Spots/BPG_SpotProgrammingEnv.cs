using UnityEngine;

namespace nitou.BlockPG.DragDrop {
    using nitou.BlockPG.Interface;

    public sealed class BPG_SpotProgrammingEnv : BPG_Spot {

        private I_BPG_ProgrammingEnv _programmingEnv;

        public override Vector2 DropPosition => Vector2.zero;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        protected override void OnEnable() {
            base.OnEnable();
            _programmingEnv = GetComponent<I_BPG_ProgrammingEnv>();
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        public I_BPG_ProgrammingEnv GetProgrammingEnv() {
            return _programmingEnv;
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnValidate() {
            RectTransform.pivot = new Vector2(0.5f, 0.5f);
        }
#endif
    }
}
