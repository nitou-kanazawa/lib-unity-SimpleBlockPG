using TMPro;
using UnityEngine;

namespace nitou.BlockPG.Blocks.Section {

    /// <summary>
    /// 入力欄（<see cref="TMP_InputField"/>）と対応する入力要素の基底クラス．
    /// </summary>
    /// <remarks>
    /// [NOTE] 入力欄が未設定でも値の保持・復元は成立する．
    ///        見た目を伴わない状態（テストやヘッドレスな復元）でも壊れないようにするため．
    /// </remarks>
    public abstract class BPG_BlockSectionHeader_InputFieldBase : BPG_BlockSectionHeader_InputBase {

        [SerializeField] TMP_InputField _inputField;

        /// <summary>
        /// 対応する入力欄．（※未設定の場合はnull）
        /// </summary>
        /// <remarks>
        /// [NOTE] 参照が未設定なら都度探す．入力欄を後から付ける場合があり、
        ///        Awake の時点だけで決めるとコンポーネントの追加順に依存してしまうため．
        /// </remarks>
        public TMP_InputField InputField {
            get {
                if (_inputField != null)
                    return _inputField;

                _inputField = GetComponentInChildren<TMP_InputField>(includeInactive: true);

                // [NOTE] onValueChanged ではなく onEndEdit で受ける．
                //        1打鍵ごとに正規化すると、数値入力で "-" や "1." のような入力途中の状態が
                //        その場で丸められてしまい、まともに打てなくなるため．
                if (_inputField != null) {
                    _inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
                }
                return _inputField;
            }
        }


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        protected override void Awake() {
            // ※初期値の反映は入力欄の解決後に行う
            _ = InputField;
            base.Awake();
        }

        protected virtual void OnDestroy() {
            if (_inputField != null) {
                _inputField.onEndEdit.RemoveListener(OnInputFieldEndEdit);
            }
        }


        /// ----------------------------------------------------------------------------
        // Protected Method

        protected override void ApplyToView(string value) {
            var field = InputField;
            if (field == null)
                return;

            // ※通知つきで設定すると SetValueFromView が再入するため、通知なしで書き戻す
            if (field.text != value) {
                field.SetTextWithoutNotify(value);
            }
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private void OnInputFieldEndEdit(string value) {
            SetValueFromView(value);
        }
    }
}
