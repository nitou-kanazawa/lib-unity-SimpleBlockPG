using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace nitou.BlockPG.Blocks.Section {
    using nitou.BlockPG.Interface;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    //[RequireComponent(typeof(Shadow))]
    public sealed class BPG_BlockSectionHeader : BPG_ComponentBase, 
        I_BPG_BlockSectionHeader {

        // refecences (self)
        private Image _image;
        
        // references (parent)
        private I_BPG_BlockSection _section;
        private I_BPG_BlockLayout _blockLayout;
        
        // references (children)
        private readonly List<I_BPG_BlockSectionHeaderItem> _items = new();

        [SerializeField] float _minHeight = 105f;
        // [NOTE] 誤字を直した際、既存プレハブの値を失わないよう旧名を残す．
        [FormerlySerializedAs("_minWidht")]
        [SerializeField] float _minWidth = 105f;
        [SerializeField] float _paddingRight = 0f;
        [SerializeField] float _spacing = 15f;

        // [NOTE] アイテムの横並びは自前で行うため、余白もここで持つ．
        //        既定値は撤去した HorizontalLayoutGroup の設定と同じ．
        [SerializeField] float _paddingLeft = 15f;
        [SerializeField] float _paddingTop = 10f;
        [SerializeField] float _paddingBottom = 20f;

        // ※アイテムは縦中央に揃える（撤去した LayoutGroup の MiddleLeft 相当）
        private static readonly Vector2 ItemAlignment = new(0f, 0.5f);


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>
        /// サイズ情報．
        /// </summary>
        public Vector2 Size {
            get => RectTransform.sizeDelta;
            set => RectTransform.sizeDelta = value;
        }

        /// <summary>
        /// Header items exist in target section header.
        /// </summary>
        public IList<I_BPG_BlockSectionHeaderItem> Items => _items;

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

            UpdateItems();
            UpdateInputs();

            IsInitialized = true;
        }

        /// <summary>
        /// Updates the layout of an individual block header. Used to correctly resize the body after adding operation blocks
        /// </summary>
        [ContextMenu("Update Layout")]
        public void UpdateLayout() {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                UpdateItems();
            }
#endif
            UpdateSelfSize();
            ApplyColor();

            // ※アイテムを横に並べる（サイズ確定後に行う）
            BPG_LayoutUtils.StackChildren(RectTransform, new BPG_StackSettings(
                vertical: false,
                spacing: _spacing,
                paddingLeft: _paddingLeft,
                paddingRight: _paddingRight,
                paddingTop: _paddingTop,
                paddingBottom: _paddingBottom,
                alignment: ItemAlignment));
        }

        /// <summary>
        /// Updates the ItemsArray with all the current I_BE2_BlockSectionHeaderItem (labels and inputs) in the header
        /// </summary>
        public void UpdateItems() {
            _items.Clear();

            // 直下のアクティブなアイテムを取得する
            foreach (Transform chiled in transform) {
                if(chiled.TryGetComponent<I_BPG_BlockSectionHeaderItem>(out var item)
                    && item.RectTransform.gameObject.activeSelf) {
                        _items.Add(item);
                }
            }
        }

        /// <summary>
        /// Updates the InputsArray with all the current I_BE2_BlockSectionHeaderInput (inputs only) in the header 
        /// </summary>
        public void UpdateInputs() {

        }


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        /// <summary>
        /// 子オブジェクト（ヘッダーアイテム）の追加・削除・並び替えを検知する．
        /// </summary>
        private void OnTransformChildrenChanged() {
            UpdateItems();
            MarkLayoutDirty();
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private void MarkLayoutDirty() {
            // ※Initialize前に呼ばれる場合があるため、未取得なら取得を試みる
            if (_blockLayout == null) {
                GatherComponents();
            }
            _blockLayout?.SetLayoutDirty();
        }

        private void GatherComponents() {

            _image = GetComponent<Image>();

            // parents
            // [NOTE] 階層構造は Block > Section > Header を前提とする．
            //        ドラッグ中の再ペアレントなどで前提が崩れる場合があるため、各階層を確認する．
            var sectionTransform = transform.parent;
            if (sectionTransform != null) {
                _section = sectionTransform.GetComponent<I_BPG_BlockSection>();

                var blockTransform = sectionTransform.parent;
                _blockLayout = (blockTransform != null)
                    ? blockTransform.GetComponent<I_BPG_BlockLayout>()
                    : null;
            } else {
                _section = null;
                _blockLayout = null;
            }
        }

        private void ApplyColor() {
            if (_image != null && _image.sprite != null && _blockLayout != null) {
                _image.color = _blockLayout.Color;
            }
        }

        private void UpdateSelfSize() {
            // 所属セクションが不明な場合はサイズを決定できない
            if (_section == null)
                return;

            bool isFirstSection = _section.RectTransform.GetSiblingIndex() == 0;

            float width = 0, height = 0;
            if (isFirstSection) {
                // width
                float w = _items.Sum(item => item.Size.x + _spacing) + _spacing + _paddingRight;
                width = Mathf.Max(_minWidth, w);

                // height
                float h = _items.Any() ? _items.Max(item => item.Size.y) : 0;
                height = Mathf.Max(_minHeight, (_minHeight - 40) + h);

            } else {
                // ※2つ目以降のセクションは、1つ目のヘッダーに幅を揃える
                var firstHeader = (_blockLayout != null && _blockLayout.Sections.Count > 0)
                    ? _blockLayout.Sections[0].Header
                    : null;

                // 参照できない場合は現在の幅を維持する
                width = (firstHeader != null) ? firstHeader.Size.x : RectTransform.sizeDelta.x;
                height = RectTransform.sizeDelta.y;
            }

            // apply
            RectTransform.sizeDelta = new Vector2(width, height);
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnValidate() {
            GatherComponents();
        }
#endif
    }
}



