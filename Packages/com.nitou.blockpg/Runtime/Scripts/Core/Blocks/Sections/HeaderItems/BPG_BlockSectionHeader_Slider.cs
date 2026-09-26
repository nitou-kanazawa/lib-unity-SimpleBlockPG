using System;
using System.Globalization;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace nitou.BlockPG.Blocks.Section {

    /// <summary>
    /// スライダーで数値を選ぶ入力要素．
    /// </summary>
    /// <remarks>
    /// 値は範囲（<see cref="Min"/>〜<see cref="Max"/>）に丸め、整数指定なら四捨五入する．
    /// 文字列化にはインバリアントカルチャを使う（<see cref="BPG_BlockSectionHeader_NumberInput"/> と同じ理由）．
    ///
    /// [NOTE] スライダーが未設定でも値の保持・復元は成立する．
    ///        ウィジェットの参照が設定済みの場合も Awake で必ず購読する（参照未設定のときだけ探して購読する作りだと、
    ///        プレハブ生成で参照を先に入れたときに操作が値へ届かない）．
    /// </remarks>
    public sealed class BPG_BlockSectionHeader_Slider : BPG_BlockSectionHeader_InputBase {

        [SerializeField] Slider _slider;

        /// <summary>現在値を表示するラベル．（※無くてもよい）</summary>
        [SerializeField] TMP_Text _valueLabel;

        [SerializeField] float _min = 0f;
        [SerializeField] float _max = 1f;

        /// <summary>整数に丸めるかどうか．</summary>
        [SerializeField] bool _wholeNumbers = true;

        /// <summary>ラベルの書式（{0} が値）．</summary>
        [SerializeField] string _labelFormat = "{0}";

        /// <summary>onValueChanged の購読．</summary>
        IDisposable _subscription;


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>
        /// 対応するスライダー．（※未設定の場合はnull）
        /// </summary>
        /// <remarks>
        /// [NOTE] 参照が未設定なら都度探す．スライダーを後から付ける場合があり、
        ///        Awake の時点だけで決めるとコンポーネントの追加順に依存してしまうため．
        /// </remarks>
        public Slider Slider {
            get {
                if (_slider != null)
                    return _slider;

                _slider = GetComponentInChildren<Slider>(includeInactive: true);
                Subscribe();
                return _slider;
            }
        }

        /// <summary>
        /// 数値としての入力値．
        /// </summary>
        public float NumberValue => Parse(Value);

        public float Min => _min;
        public float Max => _max;
        public bool WholeNumbers => _wholeNumbers;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        protected override void Awake() {
            // ※初期値の反映はスライダーの解決後に行う
            _ = Slider;
            Subscribe();
            base.Awake();
        }

        private void OnDestroy() {
            _subscription?.Dispose();
            _subscription = null;
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 範囲を設定する．現在値は新しい範囲へ丸め直す．
        /// </summary>
        public void SetRange(float min, float max, bool wholeNumbers) {
            _min = Mathf.Min(min, max);
            _max = Mathf.Max(min, max);
            _wholeNumbers = wholeNumbers;
            SetValue(Value);
        }

        /// <summary>
        /// 数値で入力値を設定する．
        /// </summary>
        public void SetNumber(float value) {
            SetValue(value.ToString(CultureInfo.InvariantCulture));
        }


        /// ----------------------------------------------------------------------------
        // Protected Method

        /// <summary>
        /// 数値として解釈できない入力は 0 とみなし、範囲内へ丸める．
        /// </summary>
        protected override string Normalize(string value) {
            float number = Mathf.Clamp(Parse(value), _min, _max);
            if (_wholeNumbers) {
                number = Mathf.Round(number);
            }
            return number.ToString(CultureInfo.InvariantCulture);
        }

        protected override void ApplyToView(string value) {
            float number = Parse(value);

            var slider = Slider;
            if (slider != null) {
                // ※範囲はこちらが正．スライダー側がずれていれば合わせる
                if (!Mathf.Approximately(slider.minValue, _min)) slider.minValue = _min;
                if (!Mathf.Approximately(slider.maxValue, _max)) slider.maxValue = _max;
                if (slider.wholeNumbers != _wholeNumbers) slider.wholeNumbers = _wholeNumbers;

                // ※通知つきで設定すると SetValueFromView が再入するため、通知なしで書き戻す
                if (!Mathf.Approximately(slider.value, number)) {
                    slider.SetValueWithoutNotify(number);
                }
            }

            UpdateLabel(number);
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// スライダーの onValueChanged を一度だけ購読する．
        /// </summary>
        private void Subscribe() {
            if (_subscription != null || _slider == null)
                return;
            _subscription = _slider.onValueChanged.AsObservable()
                .Subscribe(OnSliderValueChanged)
                .AddTo(this);
        }

        private void OnSliderValueChanged(float value) {
            SetValueFromView(value.ToString(CultureInfo.InvariantCulture));
            // ※操作経路では入力欄への書き戻しを行わない（基底の方針）が、ラベルは操作中も追従させる
            UpdateLabel(NumberValue);
        }

        /// <summary>
        /// 現在値のラベルを更新する．（※ラベルが無ければ何もしない）
        /// </summary>
        private void UpdateLabel(float number) {
            if (_valueLabel == null)
                return;
            _valueLabel.text = string.Format(CultureInfo.InvariantCulture, string.IsNullOrEmpty(_labelFormat) ? "{0}" : _labelFormat, number);
        }

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
