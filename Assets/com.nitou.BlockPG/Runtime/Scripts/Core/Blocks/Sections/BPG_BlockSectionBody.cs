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
        // - ブロックの表示順は LayoutGroup によって管理している．
        // - 子ブロックのリストとサイズは、構成が変化した時にのみ更新する．
        //   （OnTransformChildrenChanged で検知し、次のフレームでまとめて反映される）

        private Image _image;
        private BPG_SpotBlockBody _spot;

        private I_BPG_BlockSection _section;
        private I_BPG_BlockLayout _blockLayout;

        // references (children)
        private readonly List<I_BPG_Block> _childBlocks = new();


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
            UpdateChildBlocks();    // ※親からの再帰更新時にもリストを取り直す
            UpdateSelfSize();
            ApplyColor();
        }

        /// <summary>
        /// Updates ChildBlocksCount and ChildBlocksArray with the current child blocks.
        /// </summary>
        public void UpdateChildBlocks() {
            _childBlocks.Clear();

            // 直下のアクティブなブロックを取得する
            foreach (Transform child in transform) {
                if (child.gameObject.activeSelf
                    && child.TryGetComponent<I_BPG_Block>(out var block)) {
                    _childBlocks.Add(block);
                }
            }
        }


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        /// <summary>
        /// 子オブジェクトの追加・削除・並び替えを検知する．
        /// [NOTE] ブロックの接続／切断はすべて再ペアレントとして現れるため、
        ///        個々の操作を呼び出し元で追跡せずにここで一括検知する．
        /// </summary>
        private void OnTransformChildrenChanged() {
            UpdateChildBlocks();
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
            _spot = GetComponent<BPG_SpotBlockBody>();

            // parents
            // [NOTE] 階層構造は Block > Section > Body を前提とする．
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

            // ※所属ブロックが未設定の場合はトリガーブロックとして扱わない
            bool isTriggerBlock = _section.Block != null && _section.Block.IsTrigger();

            float minHeight = isTriggerBlock ? 0f : 50f;
            float height = _childBlocks.Sum(child => (child.Layout != null ? child.Layout.Size.y : 0f) - 10) - 10;

            height = Mathf.Max(minHeight, height);


            // 特定条件下で高さを加算
            // ※セクションがルート直下にある場合、親を持たない
            var sectionParent = _section.RectTransform.parent;
            bool isSecondLastSibling = sectionParent != null
                && _section.RectTransform.GetSiblingIndex() == sectionParent.childCount - 2;

            if (isSecondLastSibling && !isTriggerBlock) {
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
