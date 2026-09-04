using System.Globalization;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Blocks.Section;

namespace RuntimeTests {

    /// <summary>
    /// ヘッダーの入力要素そのものを検証する．
    /// </summary>
    public class BlockHeaderInputTest {

        private GameObject _root;

        [SetUp]
        public void SetUp() => _root = new GameObject("[Test] Input", typeof(RectTransform));

        [TearDown]
        public void TearDown() {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        private T Create<T>() where T : BPG_BlockSectionHeader_InputBase {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            return go.AddComponent<T>();
        }


        /// ----------------------------------------------------------------------------
        // 文字列入力

        [Test]
        public void 設定した文字列を保持する() {
            var input = Create<BPG_BlockSectionHeader_TextInput>();

            input.SetValue("hello");

            Assert.That(input.Value, Is.EqualTo("hello"));
        }

        [Test]
        public void nullは空文字として扱う() {
            var input = Create<BPG_BlockSectionHeader_TextInput>();

            input.SetValue(null);

            Assert.That(input.Value, Is.Empty);
        }

        [Test]
        public void 値が変わると通知が飛ぶ() {
            var input = Create<BPG_BlockSectionHeader_TextInput>();
            int count = 0;
            input.OnValueChanged += _ => count++;

            input.SetValue("a");
            input.SetValue("a");    // ※同じ値では飛ばない
            input.SetValue("b");

            Assert.That(count, Is.EqualTo(2));
        }


        /// ----------------------------------------------------------------------------
        // 数値入力

        [Test]
        public void 数値として解釈できない入力は0になる() {
            var input = Create<BPG_BlockSectionHeader_NumberInput>();

            input.SetValue("abc");

            Assert.That(input.NumberValue, Is.EqualTo(0f));
        }

        [Test]
        public void 数値を文字列として保持する() {
            var input = Create<BPG_BlockSectionHeader_NumberInput>();

            input.SetNumber(12.5f);

            Assert.That(input.Value, Is.EqualTo("12.5"));
            Assert.That(input.NumberValue, Is.EqualTo(12.5f));
        }

        [Test]
        public void 小数点がカルチャに依存しない() {
            // [NOTE] 小数点が "," になるロケールで保存すると、他の環境で読めなくなる．
            var original = Thread.CurrentThread.CurrentCulture;
            try {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

                var input = Create<BPG_BlockSectionHeader_NumberInput>();
                input.SetNumber(1.5f);

                Assert.That(input.Value, Is.EqualTo("1.5"));
                Assert.That(input.NumberValue, Is.EqualTo(1.5f));
            } finally {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }


        /// ----------------------------------------------------------------------------
        // ドロップダウン

        [Test]
        public void 選択肢の文字列を値として持つ() {
            var input = Create<BPG_BlockSectionHeader_Dropdown>();
            var dropdown = input.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.options.Add(new TMP_Dropdown.OptionData("右"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("左"));

            input.SetValue("左");

            Assert.That(input.Value, Is.EqualTo("左"));
            Assert.That(dropdown.value, Is.EqualTo(1), "表示も追従するはず．");
        }

        [Test]
        public void 選択肢に無い値は先頭へ丸める() {
            var input = Create<BPG_BlockSectionHeader_Dropdown>();
            var dropdown = input.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.options.Add(new TMP_Dropdown.OptionData("右"));
            dropdown.options.Add(new TMP_Dropdown.OptionData("左"));

            input.SetValue("上");

            Assert.That(input.Value, Is.EqualTo("右"));
        }

        [Test]
        public void 選択肢が未設定なら値をそのまま保持する() {
            // [NOTE] 選択肢を組み立てる前に丸めると、保存された値が消えてしまう．
            var input = Create<BPG_BlockSectionHeader_Dropdown>();

            input.SetValue("あとで選択肢に入る値");

            Assert.That(input.Value, Is.EqualTo("あとで選択肢に入る値"));
        }


        /// ----------------------------------------------------------------------------
        // レイアウト

        [Test]
        public void 入力要素はヘッダーのアイテムとして扱われる() {
            var input = Create<BPG_BlockSectionHeader_TextInput>();

            Assert.That(input, Is.InstanceOf<I_BPG_BlockSectionHeaderItem>());
            Assert.That(input, Is.InstanceOf<I_BPG_BlockSectionHeaderInput>());
        }
    }
}
