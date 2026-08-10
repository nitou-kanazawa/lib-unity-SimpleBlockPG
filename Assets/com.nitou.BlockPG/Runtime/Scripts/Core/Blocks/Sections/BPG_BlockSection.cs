using UnityEngine;


namespace nitou.BlockPG.Blocks.Section {
    using nitou.BlockPG.Interface;

    [DisallowMultipleComponent]
    public class BPG_BlockSection : BPG_ComponentBase, I_BPG_BlockSection {

        [SerializeField] BPG_BlockSectionHeader _header;
        [SerializeField] BPG_BlockSectionBody _body;

        /// <summary>
        /// 
        /// </summary>
        public I_BPG_Block Block { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public I_BPG_BlockSectionHeader Header => _header;

        /// <summary>
        /// 
        /// </summary>
        public I_BPG_BlockSectionBody Body => _body;


        public Vector2 Size {
            get {
                if (_header != null) {
                    var size = _header.Size;
                    if (_body != null) {
                        size.y += _body.Size.y;
                    }
                    return size;
                } else {
                    return RectTransform.sizeDelta;
                }
            }
        }


        /// ------------------------------
        /// ----------------------------------------------
        // Public Method
        
        private void Awake() {
            GatherComponents();

            if (_header != null)
                _header.Initialize();
            if (_body != null)
                _body.Initialize();
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        public void UpdateLayout() {
            if (_header != null) {
                _header.UpdateLayout();
            }
            if (_body != null) {
                _body.UpdateLayout();
            }

            RectTransform.sizeDelta = Size;
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private void GatherComponents() {
            // parents
            Block = GetComponentInParent<I_BPG_Block>();
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnValidate() {
            GatherComponents();
        }
#endif
    }
}
