using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace nitou.BlockPG.Blocks.Section {

    /// <summary>
    /// 選択肢から選ぶ入力要素．
    /// </summary>
    /// <remarks>
    /// [NOTE] 保存するのはインデックスではなく選択肢の文字列．インデックスで保存すると、
    ///        選択肢の並びを変えただけで既存データの意味が変わってしまうため．
    /// </remarks>
    public sealed class BPG_BlockSectionHeader_Dropdown : BPG_BlockSectionHeader_InputBase {

        [SerializeField] TMP_Dropdown _dropdown;

        /// <summary>
        /// 対応するドロップダウン．（※未設定の場合はnull）
        /// </summary>
        /// <remarks>
        /// [NOTE] 参照が未設定なら都度探す．ドロップダウンを後から付ける場合があり、
        ///        Awake の時点だけで決めるとコンポーネントの追加順に依存してしまうため．
        /// </remarks>
        public TMP_Dropdown Dropdown {
            get {
                if (_dropdown != null)
                    return _dropdown;

                _dropdown = GetComponentInChildren<TMP_Dropdown>(includeInactive: true);
                if (_dropdown != null) {
                    _dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
                }
                return _dropdown;
            }
        }

        /// <summary>
        /// 選択肢．
        /// </summary>
        public IEnumerable<string> Options {
            get {
                var dropdown = Dropdown;
                return (dropdown != null)
                    ? dropdown.options.Select(option => option.text)
                    : Enumerable.Empty<string>();
            }
        }


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        protected override void Awake() {
            // ※初期値の反映はドロップダウンの解決後に行う
            _ = Dropdown;
            base.Awake();
        }

        private void OnDestroy() {
            if (_dropdown != null) {
                _dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 選択肢を差し替える．
        /// </summary>
        public void SetOptions(IEnumerable<string> options) {
            var dropdown = Dropdown;
            if (dropdown == null) {
                Debug.LogWarning("Dropdown is not assigned.", this);
                return;
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(options.ToList());

            // ※選択肢が変わると、現在の値が選択肢に無くなる場合がある
            SetValue(Value);
        }


        /// ----------------------------------------------------------------------------
        // Protected Method

        /// <summary>
        /// 選択肢に無い値は先頭の選択肢へ丸める．
        /// </summary>
        /// <remarks>
        /// [NOTE] 選択肢が未設定の間は丸めない．プレハブに保存された値が、
        ///        選択肢を組み立てる前に消えてしまうため．
        /// </remarks>
        protected override string Normalize(string value) {
            value ??= string.Empty;

            var dropdown = Dropdown;
            if (dropdown == null || dropdown.options.Count == 0)
                return value;

            return (IndexOfOption(value) >= 0) ? value : dropdown.options[0].text;
        }

        protected override void ApplyToView(string value) {
            var dropdown = Dropdown;
            if (dropdown == null)
                return;

            int index = IndexOfOption(value);

            // ※通知つきで設定すると SetValueFromView が再入するため、通知なしで書き戻す
            if (index >= 0 && dropdown.value != index) {
                dropdown.SetValueWithoutNotify(index);
            }
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private int IndexOfOption(string value) {
            return Dropdown.options.FindIndex(option => option.text == value);
        }

        private void OnDropdownValueChanged(int index) {
            var dropdown = Dropdown;
            if (dropdown == null || index < 0 || index >= dropdown.options.Count)
                return;

            SetValueFromView(dropdown.options[index].text);
        }
    }
}
