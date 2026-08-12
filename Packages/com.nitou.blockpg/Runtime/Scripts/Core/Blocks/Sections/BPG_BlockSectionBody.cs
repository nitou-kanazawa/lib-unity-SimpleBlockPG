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


        // [NOTE] 子ブロックの配置は自前で行うため、余白と間隔もここで持つ．
        //        既定値は撤去した VerticalLayoutGroup の設定と同じ．
        //        間隔が負なのは、ブロック同士を接続部の凹凸ぶん食い込ませるため．
        [SerializeField] float _spacing = -10f;
        [SerializeField] float _paddingLeft = 20f;
        [SerializeField] float _paddingRight = 0f;
        [SerializeField] float _paddingTop = -10f;
        [SerializeField] float _paddingBottom = 0f;

        /// <summary>子ブロックが無い場合に確保する最小の長さ．（※接続先として掴める大きさ）</summary>
        private const float MIN_LENGTH = 50f;

        /// <summary>最後のセクションで末端の見た目のぶん加算する長さ．</summary>
        private const float END_CAP_LENGTH = 50f;


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
            UpdateChildLayouts();
            UpdateSelfSize();
            ApplyColor();

            // ※子ブロックを並べる（サイズ確定後に行う）
            BPG_LayoutUtils.StackChildren(RectTransform, new BPG_StackSettings(
                vertical: (_section?.Axis ?? BlockLayoutAxis.Vertical).IsVertical(),
                spacing: _spacing,
                paddingLeft: _paddingLeft,
                paddingRight: _paddingRight,
                paddingTop: _paddingTop,
                paddingBottom: _paddingBottom));
        }

        /// <summary>
        /// 子ブロックのレイアウトを更新する．
        /// [NOTE] レイアウト更新はルートブロックからの一度の再帰で部分木全体を揃える設計のため、
        ///        ここで子ブロックへ降りないと入れ子のブロックが一切更新されない．
        ///        （子ブロック自身の LateUpdate は、親を持つ場合に早期リターンする）
        ///        また自身のサイズは子のサイズの合計に依存するため、必ず子を先に更新する．
        /// </summary>
        private void UpdateChildLayouts() {
            foreach (var childBlock in _childBlocks) {
                childBlock?.Layout?.UpdateLayout();
            }
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

            var axis = _section.Axis;

            // ※所属ブロックが未設定の場合はトリガーブロックとして扱わない
            bool isTriggerBlock = _section.Block != null && _section.Block.IsTrigger();

            float minLength = isTriggerBlock ? 0f : MIN_LENGTH;
            float length = _childBlocks.Sum(child =>
                (child.Layout != null ? axis.Along(child.Layout.Size) : 0f) + _spacing) + _spacing;

            length = Mathf.Max(minLength, length);


            // 末端の見た目（スコープブロックの下辺）は最後のセクションが担うため、そのぶんを足す
            if (IsLastSection() && !isTriggerBlock) {
                length += END_CAP_LENGTH;
            }

            // apply
            RectTransform.sizeDelta = axis.ToSize(length, axis.Across(_section.Size));
        }

        /// <summary>
        /// 所属セクションがブロック内の最後のセクションかどうか．
        /// </summary>
        /// <remarks>
        /// [NOTE] 以前は「兄弟の末尾から2番目か」で判定していたが、これは
        ///        ブロック直下の最後の子が OuterArea であることへの暗黙の依存だった．
        ///        プレハブに子を1つ足すだけで壊れるため、セクションの並びで判定する．
        /// </remarks>
        private bool IsLastSection() {
            if (_blockLayout == null)
                return false;

            var sections = _blockLayout.Sections;
            return sections.Count > 0
                && ReferenceEquals(sections[sections.Count - 1], _section);
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
