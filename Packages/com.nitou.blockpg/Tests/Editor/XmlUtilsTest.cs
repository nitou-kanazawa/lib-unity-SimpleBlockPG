using System.Globalization;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using nitou.BlockPG.Serialization;

namespace EditorTests {

    /// <summary>
    /// <see cref="XmlUtils"/>の変換処理に関するテスト．
    /// </summary>
    public class XmlUtilsTest {

        private CultureInfo _originalCulture;

        [SetUp]
        public void SetUp() {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
        }

        [TearDown]
        public void TearDown() {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
        }


        /// ----------------------------------------------------------------------------
        // Vector3 -> string

        [Test]
        public void Vector3ToString_小数点以下が丸められずに保持される() {
            // [NOTE] Vector3.ToString() は小数点以下2桁に丸めるため、独自実装が必要になっている．
            var value = new Vector3(1.23456789f, -0.000123f, 12345.678f);

            var result = XmlUtils.Vector3ToString(value);
            var restored = XmlUtils.StringToVector3(result);

            Assert.That(restored.x, Is.EqualTo(value.x));
            Assert.That(restored.y, Is.EqualTo(value.y));
            Assert.That(restored.z, Is.EqualTo(value.z));
        }

        [Test]
        public void Vector3ToString_小数点にカンマを使うロケールでも区切り文字と衝突しない() {
            // Arrange : ドイツ語ロケールでは小数点がカンマになる
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            var value = new Vector3(1.5f, 2.5f, 3.5f);

            // Act
            var result = XmlUtils.Vector3ToString(value);

            // Assert : 区切り文字のカンマは2つだけ（小数点がカンマ化していない）
            Assert.That(result.Split(',').Length, Is.EqualTo(3));
            Assert.That(result, Does.Contain("1.5"));
        }


        /// ----------------------------------------------------------------------------
        // string -> Vector3

        [Test]
        public void StringToVector3_保存した文字列を元の値へ戻せる() {
            var value = new Vector3(10.5f, -20.25f, 0f);

            var restored = XmlUtils.StringToVector3(XmlUtils.Vector3ToString(value));

            Assert.That(restored, Is.EqualTo(value));
        }

        [Test]
        public void StringToVector3_カンマ小数点ロケールでもInvariantCultureで解釈する() {
            // Arrange
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            // Act
            var restored = XmlUtils.StringToVector3("(1.5, 2.5, 3.5)");

            // Assert
            Assert.That(restored, Is.EqualTo(new Vector3(1.5f, 2.5f, 3.5f)));
        }

        [Test]
        public void StringToVector3_括弧が無くても解釈できる() {
            var restored = XmlUtils.StringToVector3("1, 2, 3");

            Assert.That(restored, Is.EqualTo(new Vector3(1f, 2f, 3f)));
        }

        // [NOTE] 不正入力時は警告ログを出して既定値を返す仕様のため、例外は送出されない．
        //        （警告は LogAssert の既定ではテスト失敗にならない）

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void StringToVector3_空入力ならゼロを返す(string input) {
            var restored = XmlUtils.StringToVector3(input);

            Assert.That(restored, Is.EqualTo(Vector3.zero));
        }

        [TestCase("(1, 2)")]
        [TestCase("(1, 2, 3, 4)")]
        public void StringToVector3_要素数が3でなければゼロを返す(string input) {
            var restored = XmlUtils.StringToVector3(input);

            Assert.That(restored, Is.EqualTo(Vector3.zero));
        }

        [TestCase("(a, b, c)")]
        [TestCase("(1, two, 3)")]
        public void StringToVector3_数値に変換できなければゼロを返す(string input) {
            var restored = XmlUtils.StringToVector3(input);

            Assert.That(restored, Is.EqualTo(Vector3.zero));
        }
    }
}
