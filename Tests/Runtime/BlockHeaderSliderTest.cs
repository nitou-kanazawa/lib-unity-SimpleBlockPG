using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using nitou.BlockPG.Blocks.Section;

namespace RuntimeTests {

    /// <summary>
    /// スライダー入力要素を検証する．
    /// </summary>
    public class BlockHeaderSliderTest {

        private GameObject _root;

        [SetUp]
        public void SetUp() => _root = new GameObject("[Test] Slider", typeof(RectTransform));

        [TearDown]
        public void TearDown() {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private BPG_BlockSectionHeader_Slider Create() {
            var go = new GameObject(nameof(BPG_BlockSectionHeader_Slider), typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            return go.AddComponent<BPG_BlockSectionHeader_Slider>();
        }


        [Test]
        public void 範囲の外は端に丸める() {
            var input = Create();
            input.SetRange(1f, 5f, wholeNumbers: true);

            input.SetValue("9");
            Assert.That(input.Value, Is.EqualTo("5"));

            input.SetValue("-3");
            Assert.That(input.Value, Is.EqualTo("1"));
        }

        [Test]
        public void 整数指定なら四捨五入する() {
            var input = Create();
            input.SetRange(1f, 5f, wholeNumbers: true);

            input.SetValue("3.6");

            Assert.That(input.Value, Is.EqualTo("4"));
            Assert.That(input.NumberValue, Is.EqualTo(4f));
        }

        [Test]
        public void 数値として解釈できない入力は最小値になる() {
            var input = Create();
            input.SetRange(1f, 5f, wholeNumbers: true);

            input.SetValue("abc");

            Assert.That(input.Value, Is.EqualTo("1"));
        }

        [Test]
        public void スライダーの操作が値に届く() {
            var input = Create();
            var slider = input.gameObject.AddComponent<Slider>();
            input.SetRange(1f, 5f, wholeNumbers: true);   // ※Slider の解決と購読はここで行われる

            slider.value = 3f;   // ※ユーザーの操作と同じ経路（onValueChanged）

            Assert.That(input.Value, Is.EqualTo("3"));
            Assert.That(slider.minValue, Is.EqualTo(1f));
            Assert.That(slider.maxValue, Is.EqualTo(5f));
        }

        [Test]
        public void 値を設定するとスライダーへ通知なしで反映される() {
            var input = Create();
            var slider = input.gameObject.AddComponent<Slider>();
            input.SetRange(1f, 5f, wholeNumbers: true);
            int count = 0;
            input.OnValueChanged += _ => count++;

            input.SetValue("4");

            Assert.That(slider.value, Is.EqualTo(4f));
            Assert.That(count, Is.EqualTo(1), "SetValue の通知は 1 回。書き戻しで再入しない");
        }

        [Test]
        public void スライダーの操作でラベルも追従する() {
            var input = Create();
            var slider = input.gameObject.AddComponent<Slider>();
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(_root.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BPG_BlockSectionHeader_Slider).GetField("_valueLabel", flags).SetValue(input, label);
            typeof(BPG_BlockSectionHeader_Slider).GetField("_labelFormat", flags).SetValue(input, "{0} m");
            input.SetRange(1f, 5f, wholeNumbers: true);

            slider.value = 3f;   // ※ユーザーの操作と同じ経路

            Assert.That(input.Value, Is.EqualTo("3"));
            Assert.That(label.text, Is.EqualTo("3 m"), "操作中はラベルだけ追従する（入力欄への書き戻しはしない）");
        }
    }
}
