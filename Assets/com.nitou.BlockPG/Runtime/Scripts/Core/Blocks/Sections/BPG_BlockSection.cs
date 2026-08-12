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


        /// <summary>
        /// 積み上げ方向．（※所属ブロックのレイアウトに従う）
        /// </summary>
        public BlockLayoutAxis Axis => Block?.Layout?.Axis ?? BlockLayoutAxis.Vertical;

        /// <summary>
        /// 折り畳まれているかどうか．
        /// </summary>
        public bool IsCollapsed => _body != null && !_body.gameObject.activeSelf;

        /// <summary>
        /// セクション全体のサイズ．
        /// </summary>
        /// <remarks>
        /// ヘッダーを基準に、積み上げ方向へボディのぶんを足す．
        /// 直交方向はヘッダーの値をそのまま使う．
        /// </remarks>
        public Vector2 Size {
            get {
                if (_header == null)
                    return RectTransform.sizeDelta;

                var axis = Axis;
                var headerSize = _header.Size;
                float along = axis.Along(headerSize);

                // ※折り畳みなどで非表示にされたボディは寸法に含めない
                if (_body != null && _body.gameObject.activeSelf) {
                    along += axis.Along(_body.Size);
                }
                return axis.ToSize(along, axis.Across(headerSize));
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

            // ※ヘッダーとボディを並べる（サイズ確定後に行う）
            BPG_LayoutUtils.StackChildren(RectTransform, new BPG_StackSettings(vertical: Axis.IsVertical()));
        }

        /// <summary>
        /// 折り畳み状態を設定する．
        /// </summary>
        /// <remarks>
        /// [NOTE] ボディを非表示にすることで実現する．子ブロックの activeSelf は
        ///        true のまま残るため、畳んだ状態で保存しても中身は失われない．
        ///        非表示化は階層の変化を伴わないため、更新は明示的に予約する．
        /// </remarks>
        public void SetCollapsed(bool collapsed) {
            // ※ボディを持たないセクションは畳めない
            if (_body == null)
                return;

            if (_body.gameObject.activeSelf != collapsed)
                return;

            _body.gameObject.SetActive(!collapsed);

            if (Block?.Layout != null) {
                Block.Layout.SetLayoutDirty();
            }
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
            BPG_LayoutGroupGuard.WarnIfConflicting(this);
        }
#endif
    }
}
