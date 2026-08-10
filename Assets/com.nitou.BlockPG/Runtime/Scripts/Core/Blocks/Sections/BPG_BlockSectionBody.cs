using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace nitou.BlockPG.Blocks.Section {
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.DragDrop;
    using System.Linq;

    [RequireComponent(typeof(BPG_SpotBlockBody))]
    [DisallowMultipleComponent]
    public class BPG_BlockSectionBody : BPG_ComponentBase, 
        I_BPG_BlockSectionBody {

        // [NOTE]
        // - ブロックの表示順をLayoutGroupによって管理しているため、リストは毎フレーム更新する実装となっている．
        // - 同様にサイズ更新もLayoutGroupに依存するため、場合によっては１F遅延するかも．

        private Image _image;
        private BPG_SpotBlockBody _spot;

        private I_BPG_BlockSection _section;
        private I_BPG_BlockLayout _blockLayout;

        // references (children)
        private readonly List<I_BPG_Block> _childBlocks = new();


        private static float HEIGHT_SPCING = 10;


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>
        /// サイズ情報．
        /// </summary>
        public Vector2 Size {
            get => RectTransform.sizeDelta;
            set => RectTransform.sizeDelta = value;
        }

        public I_BPG_BlockSection BlockSection => _section;

        /// <summary>
        /// 接続されている子ブロックのリスト．
        /// </summary>
        public IReadOnlyList<I_BPG_Block> ChildBlocks => _childBlocks;

        /// <summary>
        /// ブロック接続の可否判定用コンポーネント．
        /// </summary>
        public I_BPG_Spot Spot => _spot;

        /// <summary>
        /// 初期化処理が完了しているかどうか．
        /// </summary>
        public bool IsInitialized { get; private set; } = false;


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 開始処理．
        /// </summary>
        internal void Initialize() {
            if (IsInitialized)
                throw new System.InvalidOperationException("Block Header is already initialized yet.");

            GatherComponents();

            if (_image != null) {
                _image.type = Image.Type.Sliced;
                _image.pixelsPerUnitMultiplier = 2;
            }

            IsInitialized = true;
        }

        /// <summary>
        /// Updates the layout of an indivisual block section. Used to correctly resize the section after adding child and operation blocks
        /// </summary>
        [ContextMenu("Update Layout")]
        public void UpdateLayout() {
            UpdateChildBlocks();    // ※毎フレーム、ブロックリストは更新する
            UpdateSelfSize();
            ApplyColor();
        }

        /// <summary>
        /// Updates ChildBlocksCount and ChildBlocksArray with the current child blocks.
        /// </summary>
        public void UpdateChildBlocks() {
            _childBlocks.Clear();

            // 直下のアクティブなブロックを取得する
            foreach (Transform chiled in transform) {
                if (chiled.gameObject.activeSelf
                    && chiled.TryGetComponent<I_BPG_Block>(out var block)) {
                    _childBlocks.Add(block);
                }
            }
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private void GatherComponents() {
            _image = GetComponent<Image>();
            _spot = GetComponent<BPG_SpotBlockBody>();

            // parents
            if (transform.parent != null) {
                _section = transform.parent.GetComponent<I_BPG_BlockSection>();
                _blockLayout = transform.parent.parent.GetComponent<I_BPG_BlockLayout>();
            }
        }

        private void ApplyColor() {
            if (_image != null && _image.sprite != null && _blockLayout != null) {
                _image.color = _blockLayout.Color;
            }
        }

        private void UpdateSelfSize() {

            float minHeight = _section.Block.IsTrigger() ? 0f : 50f;
            float height = _childBlocks.Sum(child => child.Layout.Size.y - 10) - 10;

            height = Mathf.Max(minHeight, height);


            // 特定条件下で高さを加算
            bool isSecondLastSibling =
                _section.RectTransform.GetSiblingIndex() == _section.RectTransform.parent.childCount - 2;

            if (isSecondLastSibling && !_section.Block.IsTrigger()) {
                height += 50;
            }

            // apply
            RectTransform.sizeDelta = new Vector2(_section.Size.x, height);
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnValidate() {
            GatherComponents();
        }
#endif
    }
}
