using System.Collections.Generic;
using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// ブロック階層のレイアウトを担う基底クラス．
    /// </summary>
    /// <remarks>
    /// 積み上げ方向以外の挙動（更新の予約と伝播、セクションの収集、再帰更新）は
    /// 方向によらないため、ここへまとめる．
    ///
    /// [NOTE] 更新は「子が先、自分が後」の順序を守ること．
    ///        <see cref="Size"/> はセクションの現在のサイズを合計する計算プロパティのため、
    ///        順序を逆にすると自身のサイズだけが1回前の値のまま残る．
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class BPG_BlockLayoutBase : BPG_ComponentBase, I_BPG_BlockLayout {

        [SerializeField] Color _blockColor = Color.white;
        [SerializeField] bool _highlight = false;

        private readonly List<I_BPG_BlockSection> _sections = new();

        // [NOTE] レイアウト更新はブロックの部分木全体を走査するため、毎フレーム実行すると
        //        ブロック数に対して計算量が急増する．構成が変化した時だけ更新する．
        private bool _isLayoutDirty = true;


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>
        /// 積み上げ方向．
        /// </summary>
        public abstract BlockLayoutAxis Axis { get; }

        /// <summary>
        /// ブロックの表示色．
        /// </summary>
        public Color Color {
            get => _blockColor;
            set => _blockColor = value;
        }

        public bool Highlight {
            get => _highlight;
            set => _highlight = value;
        }

        /// <summary>
        /// 子セクション．
        /// </summary>
        public IReadOnlyList<I_BPG_BlockSection> Sections => _sections;

        /// <summary>
        /// ブロック全体のサイズ．
        /// </summary>
        /// <remarks>
        /// 積み上げ方向はセクションの合計、直交方向は最大値を取る．
        /// </remarks>
        public Vector2 Size {
            get {
                float along = 0f;
                float across = 0f;
                for (int i = 0; i < _sections.Count; i++) {
                    var size = _sections[i].Size;
                    along += Axis.Along(size);
                    across = Mathf.Max(across, Axis.Across(size));
                }
                return Axis.ToSize(along, across);
            }
        }


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        protected virtual void Awake() {
            GatherSections();
        }

        protected virtual void Start() {
            RectTransform.pivot = new Vector2(0f, 1f);
            UpdateLayout();
        }

        private void LateUpdate() {
#if UNITY_EDITOR
            // [NOTE] 編集モードではインスペクタ操作を即座に反映したいため、常に更新する．
            if (!Application.isPlaying) {
                UpdateLayout();
                return;
            }
#endif
            if (!_isLayoutDirty)
                return;

            _isLayoutDirty = false;

            // [NOTE] 子孫の変更は SetLayoutDirty() で祖先まで伝播するため、
            //        ルート側から一度だけ再帰更新すれば部分木全体が揃う．
            //        （各ブロックが個別に更新すると同じ部分木を何度も走査してしまう）
            if (HasParentLayout())
                return;

            UpdateLayout();
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 直下のセクションを収集する．
        /// </summary>
        public void GatherSections() {
            _sections.Clear();
            foreach (Transform child in transform) {
                if (child.TryGetComponent<I_BPG_BlockSection>(out var section)) {
                    _sections.Add(section);
                }
            }
        }

        /// <summary>
        /// レイアウトの再計算を予約する．
        /// </summary>
        public void SetLayoutDirty() {
            _isLayoutDirty = true;

            // 自身のサイズ変化は祖先のサイズにも影響するため、親方向へ伝播する
            GetParentLayout()?.SetLayoutDirty();
        }

        /// <summary>
        /// レイアウトを更新する．
        /// </summary>
        public void UpdateLayout() {
            // ※Size はセクションの現在のサイズを合計するため、必ずセクションを先に更新する
            for (int i = 0; i < _sections.Count; i++) {
                _sections[i]?.UpdateLayout();
            }
            RectTransform.sizeDelta = Size;

            // ※セクションと OuterArea を並べる（サイズ確定後に行う）
            BPG_LayoutUtils.StackChildren(RectTransform, new BPG_StackSettings(vertical: Axis.IsVertical()));
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// 親ブロックのレイアウトを取得する．（※ルートブロックの場合はnull）
        /// </summary>
        private I_BPG_BlockLayout GetParentLayout() {
            var parent = transform.parent;
            return (parent != null) ? parent.GetComponentInParent<I_BPG_BlockLayout>() : null;
        }

        private bool HasParentLayout() => GetParentLayout() != null;


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        protected virtual void OnValidate() {
            GatherSections();

            // ※撤去し損ねた LayoutGroup があると配置が二重に効く
            BPG_LayoutGroupGuard.WarnIfConflicting(this);
        }
#endif
    }
}
