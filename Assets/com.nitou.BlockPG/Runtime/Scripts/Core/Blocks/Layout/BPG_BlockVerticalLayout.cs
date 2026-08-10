using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using UniRx;
using UniRx.Triggers;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;

    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public sealed class BPG_BlockVerticalLayout : BPG_ComponentBase, I_BPG_BlockLayout {

        [SerializeField] Color _blockColor = Color.white;
        [SerializeField] bool _highlight = false;

        private readonly List<I_BPG_BlockSection> _sections = new();

        // [NOTE] レイアウト更新はブロックの部分木全体を走査するため、毎フレーム実行すると
        //        ブロック数に対して計算量が急増する．構成が変化した時だけ更新する．
        private bool _isLayoutDirty = true;

        /// <summary>
        /// Block visible color.
        /// </summary>
        public Color Color {
            get => _highlight ? _blockColor : _blockColor; //.WithAlpha(0.8f); 
            set => _blockColor = value;
        }

        /// <summary>
        /// Returns the size of the whole block. Headers and Bodies with child blocks are counted on.
        /// </summary>
        public Vector2 Size {
            get {
                return Sections.Aggregate(Vector2.zero, (size, section) =>
                new Vector2(
                    Mathf.Max(size.x, section.Size.x),
                    size.y + section.Size.y
                ));
            }
        }

        public bool Highlight {
            get => _highlight;
            set => _highlight = value;
        }

        /// <summary>
        /// Child sections.
        /// </summary>
        public IReadOnlyList<I_BPG_BlockSection> Sections => _sections;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        private void Awake() {
            GatherSections();
        }

        private void Start() {
            RectTransform.pivot = new Vector2(0, 1);
            UpdateLayout();
            LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);

            // use invoke repeating and remove UpdateLayout from the Uptade method if needed to increase performance 
            //InvokeRepeating("UpdateLayout", 0, 0.08f);

            // size updating
            //this.LateUpdateAsObservable()
            //    .Where(_ => this.isActiveAndEnabled)
            //    .Subscribe(_ => UpdateLayout())
            //    .AddTo(this);
        }

        private void LateUpdate() {
#if UNITY_EDITOR
            // [NOTE] 編集モードではインスペクタ操作を即座に反映したいため、常に更新する．
            //        また LayoutGroup の自動更新が走らないため、明示的に再構築する．
            if (!Application.isPlaying) {
                UpdateLayout();
                LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
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
        /// Updates the layout of the block. Used to correctly resize the blocks after adding child and operation blocks
        /// </summary>
        public void UpdateLayout() {
            RectTransform.sizeDelta = Size;
            _sections.ForEach(section => section.UpdateLayout());
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

        void OnValidate() {
            GatherSections();
        }
#endif
    }

}