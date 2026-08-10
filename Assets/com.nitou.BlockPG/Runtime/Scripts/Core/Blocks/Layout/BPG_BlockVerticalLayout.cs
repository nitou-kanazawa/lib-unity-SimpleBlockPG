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
            UpdateLayout();
#if UNITY_EDITOR
            // [NOTE] 編集モードでは LayoutGroup の自動更新が走らないため、明示的に再構築する．
            if (!Application.isPlaying) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
            }
#endif
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        public void GatherSections() {
            _sections.Clear();
            foreach (Transform chiled in transform) {
                if (chiled.TryGetComponent<I_BPG_BlockSection>(out var section)) {
                    _sections.Add(section);
                }
            }
        }

        /// <summary>
        /// Updates the layout of the block. Used to correctly resize the blocks after adding child and operation blocks
        /// </summary>
        public void UpdateLayout() {
            RectTransform.sizeDelta = Size;
            _sections.ForEach(section => section.UpdateLayout());
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR

        void OnValidate() {
            GatherSections();
        }
#endif
    }

}