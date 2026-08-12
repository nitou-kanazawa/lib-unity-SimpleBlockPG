using System;
using UnityEngine;

namespace nitou.BlockPG.Blocks.Section {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// ヘッダーに置かれる入力要素の基底クラス．
    /// </summary>
    /// <remarks>
    /// 値の保持・正規化・変更通知だけを担い、入力欄の実体には触れない．
    /// UI ウィジェットとの橋渡しは派生クラスが <see cref="ApplyToView"/> で行う．
    ///
    /// [NOTE] ウィジェットが未設定でも値は保持できるようにしてある．
    ///        保存・復元は見た目が無くても成立すべきで、テストでもその形で検証している．
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class BPG_BlockSectionHeader_InputBase : BPG_ComponentBase,
        I_BPG_BlockSectionHeaderInput {

        [SerializeField] string _value = string.Empty;

        /// <summary>
        /// サイズ情報．
        /// </summary>
        public Vector2 Size => RectTransform.sizeDelta;

        /// <summary>
        /// 現在の入力値．
        /// </summary>
        public string Value => _value;

        /// <summary>
        /// 入力値が変化した時に通知する．
        /// </summary>
        public event Action<I_BPG_BlockSectionHeaderInput> OnValueChanged;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        protected virtual void Awake() {
            _value = Normalize(_value);
            ApplyToView(_value);
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 入力値を設定する．（※セーブデータからの復元時にも使用する）
        /// </summary>
        public void SetValue(string value) {
            if (!Assign(value))
                return;

            // ※外部からの設定なので、入力欄の表示も追従させる
            ApplyToView(_value);
            NotifyChanged();
        }


        /// ----------------------------------------------------------------------------
        // Protected Method

        /// <summary>
        /// 入力欄の操作によって値が変わった時に呼ぶ．
        /// </summary>
        /// <remarks>
        /// [NOTE] <see cref="SetValue"/>と違い、入力欄への書き戻しを行わない．
        ///        入力中の欄へ書き戻すとキャレット位置が飛ぶため．
        ///        ただし正規化で値が変わった場合だけは、表示と値の食い違いを避けるため書き戻す．
        /// </remarks>
        protected void SetValueFromView(string value) {
            var normalized = Normalize(value ?? string.Empty);
            bool changed = Assign(value);

            if (normalized != value) {
                ApplyToView(_value);
            }
            if (changed) {
                NotifyChanged();
            }
        }

        /// <summary>
        /// 値を正規化する．（※不正な入力の丸めなど）
        /// </summary>
        protected virtual string Normalize(string value) => value ?? string.Empty;

        /// <summary>
        /// 入力欄の表示へ反映する．
        /// </summary>
        protected abstract void ApplyToView(string value);


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// 値を代入する．変化した場合のみtrueを返す．
        /// </summary>
        private bool Assign(string value) {
            var normalized = Normalize(value ?? string.Empty);
            if (string.Equals(_value, normalized, StringComparison.Ordinal))
                return false;

            _value = normalized;
            return true;
        }

        private void NotifyChanged() {
            OnValueChanged?.Invoke(this);

            // ※入力値によって幅が変わるため、ブロックのレイアウトを更新させる
            var layout = RectTransform.GetComponentInParent<I_BPG_BlockLayout>();
            if (layout != null) {
                layout.SetLayoutDirty();
            }
        }
    }
}
