using System.Globalization;
using UnityEngine;

namespace nitou.BlockPG.Blocks.Section {

    /// <summary>
    /// 数値を入力する要素．
    /// </summary>
    /// <remarks>
    /// [NOTE] 文字列化にはインバリアントカルチャを使う．端末のロケールに依存させると、
    ///        小数点が "," になる環境で保存したデータを他の環境で読めなくなるため．
    /// </remarks>
    public sealed class BPG_BlockSectionHeader_NumberInput : BPG_BlockSectionHeader_InputFieldBase {

        [SerializeField] float _min = float.MinValue;
        [SerializeField] float _max = float.MaxValue;

        /// <summary>整数に丸めるかどうか．</summary>
        [SerializeField] bool _isInteger = false;


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>
        /// 数値としての入力値．
        /// </summary>
        public float NumberValue => Parse(Value);


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 数値で入力値を設定する．
        /// </summary>
        public void SetNumber(float value) {
            SetValue(value.ToString(CultureInfo.InvariantCulture));
        }


        /// ----------------------------------------------------------------------------
        // Protected Method

        /// <summary>
        /// 数値として解釈できない入力は、範囲内へ丸める．
        /// </summary>
        protected override string Normalize(string value) {
            float number = Mathf.Clamp(Parse(value), _min, _max);
            if (_isInteger) {
                number = Mathf.Round(number);
            }
            return number.ToString(CultureInfo.InvariantCulture);
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// 数値として解釈する．（※解釈できない場合は0）
        /// </summary>
        private static float Parse(string value) {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : 0f;
        }
    }
}
