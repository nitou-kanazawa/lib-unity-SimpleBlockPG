using System;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Serialization;

namespace EditorTests {

    /// <summary>
    /// シリアライズ用オブジェクトとXMLの相互変換に関するテスト．
    /// </summary>
    public class SerializableBlockTest {

        /// ----------------------------------------------------------------------------
        // Helper

        private static SerializableBlock CreateBlock(string id, string name, Vector3 position) {
            return new SerializableBlock(id, name, position);
        }

        private static SerializableBlock RoundTrip(SerializableBlock sBlock) {
            return SerializableBlock.FromXElement(SerializableBlock.ToXElement(sBlock));
        }


        /// ----------------------------------------------------------------------------
        // Block

        [Test]
        public void Block_基本情報がラウンドトリップで保持される() {
            var original = CreateBlock("abc123", "Block [Normal]", new Vector3(10.5f, -20.25f, 0f));

            var restored = RoundTrip(original);

            Assert.That(restored.id, Is.EqualTo(original.id));
            Assert.That(restored.name, Is.EqualTo(original.name));
            Assert.That(restored.localPosition, Is.EqualTo(original.localPosition));
        }

        [Test]
        public void Block_入れ子構造がラウンドトリップで保持される() {
            // Arrange : root > section[0] > child > section[0] > grandChild
            var root = CreateBlock("root", "Block [Scope]", Vector3.zero);
            var child = CreateBlock("child", "Block [Normal]", Vector3.zero);
            var grandChild = CreateBlock("grandChild", "Block [Normal]", Vector3.zero);

            var childSection = new SerializableBlockSection();
            childSection.childBlocks.Add(grandChild);
            child.sections.Add(childSection);

            var rootSection = new SerializableBlockSection();
            rootSection.childBlocks.Add(child);
            root.sections.Add(rootSection);

            // Act
            var restored = RoundTrip(root);

            // Assert
            Assert.That(restored.sections.Count, Is.EqualTo(1));
            Assert.That(restored.sections[0].childBlocks.Count, Is.EqualTo(1));

            var restoredChild = restored.sections[0].childBlocks[0];
            Assert.That(restoredChild.id, Is.EqualTo("child"));
            Assert.That(restoredChild.sections[0].childBlocks[0].id, Is.EqualTo("grandChild"));
        }

        [Test]
        public void Block_複数の子ブロックの順序が保持される() {
            var root = CreateBlock("root", "Block [Scope]", Vector3.zero);
            var section = new SerializableBlockSection();
            section.childBlocks.Add(CreateBlock("first", "Block [Normal]", Vector3.zero));
            section.childBlocks.Add(CreateBlock("second", "Block [Normal]", Vector3.zero));
            section.childBlocks.Add(CreateBlock("third", "Block [Normal]", Vector3.zero));
            root.sections.Add(section);

            var restored = RoundTrip(root);

            var ids = restored.sections[0].childBlocks.Select(b => b.id).ToArray();
            Assert.That(ids, Is.EqualTo(new[] { "first", "second", "third" }));
        }

        [Test]
        public void Block_複数セクションが順序を保って復元される() {
            var root = CreateBlock("root", "Block [MultiScope]", Vector3.zero);

            var first = new SerializableBlockSection();
            first.childBlocks.Add(CreateBlock("inFirst", "Block [Normal]", Vector3.zero));
            var second = new SerializableBlockSection();
            second.childBlocks.Add(CreateBlock("inSecond", "Block [Normal]", Vector3.zero));

            root.sections.Add(first);
            root.sections.Add(second);

            var restored = RoundTrip(root);

            Assert.That(restored.sections.Count, Is.EqualTo(2));
            Assert.That(restored.sections[0].childBlocks[0].id, Is.EqualTo("inFirst"));
            Assert.That(restored.sections[1].childBlocks[0].id, Is.EqualTo("inSecond"));
        }


        /// ----------------------------------------------------------------------------
        // 欠損データへの耐性

        [Test]
        public void Block_要素が欠落していても既定値で復元される() {
            // Arrange : id / localPosition / sections が無いデータ
            var xBlock = new XElement(SerializableBlock.NAME_KEY,
                new XElement("name", "Block [Normal]"));

            // Act
            var restored = SerializableBlock.FromXElement(xBlock);

            // Assert
            Assert.That(restored.id, Is.Empty);
            Assert.That(restored.name, Is.EqualTo("Block [Normal]"));
            Assert.That(restored.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(restored.sections, Is.Empty);
        }

        [Test]
        public void Block_nullを渡すと例外を送出する() {
            Assert.Throws<ArgumentNullException>(() => SerializableBlock.FromXElement(null));
        }


        /// ----------------------------------------------------------------------------
        // Section / Input

        [Test]
        public void Section_入力値がラウンドトリップで保持される() {
            var section = new SerializableBlockSection();
            section.inputs.Add(new SerializableInput("hello"));
            section.inputs.Add(new SerializableInput("42"));

            var restored = SerializableBlockSection.FromXElement(
                SerializableBlockSection.ToXElement(section));

            Assert.That(restored.inputs.Select(i => i.value), Is.EqualTo(new[] { "hello", "42" }));
        }

        [Test]
        public void Section_childBlocksとinputsが欠落していても復元できる() {
            var xSection = new XElement(SerializableBlockSection.NAME_KEY);

            var restored = SerializableBlockSection.FromXElement(xSection);

            Assert.That(restored.childBlocks, Is.Empty);
            Assert.That(restored.inputs, Is.Empty);
        }

        [Test]
        public void Section_nullを渡すと例外を送出する() {
            Assert.Throws<ArgumentNullException>(() => SerializableBlockSection.FromXElement(null));
        }

        [Test]
        public void Input_空文字がラウンドトリップで保持される() {
            var restored = SerializableInput.FromXElement(
                SerializableInput.ToXElement(new SerializableInput(string.Empty)));

            Assert.That(restored.value, Is.Empty);
        }
    }
}
