using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Serialization;

namespace EditorTests {

    /// <summary>
    /// <see cref="BPG_BlockStorage"/>のファイル入出力に関するテスト．
    /// ※ブロック生成を伴わない範囲（シリアライズ用オブジェクトまで）を対象とする．
    /// </summary>
    public class BlockStorageTest {

        private string _path;

        [SetUp]
        public void SetUp() {
            _path = Path.Combine(Application.temporaryCachePath, "BlockPGTests", $"{Guid.NewGuid():N}.xml");
        }

        [TearDown]
        public void TearDown() {
            if (File.Exists(_path)) {
                File.Delete(_path);
            }
        }


        /// ----------------------------------------------------------------------------
        // Helper

        private static SerializableBlock CreateBlock(string id, string name) {
            return new SerializableBlock(id, name, Vector3.zero);
        }


        /// ----------------------------------------------------------------------------
        // XDocument 変換

        [Test]
        public void XDocument_ルート要素とバージョンが付与される() {
            var document = BPG_BlockStorage.ToXDocument(new[] { CreateBlock("a", "Block [Normal]") });

            Assert.That(document.Root.Name.LocalName, Is.EqualTo(BPG_BlockStorage.ROOT_KEY));
            Assert.That(document.Root.Attribute("version")?.Value, Is.EqualTo(BPG_BlockStorage.FORMAT_VERSION));
        }

        [Test]
        public void XDocument_複数のルートブロックがラウンドトリップで保持される() {
            var originals = new[] {
                CreateBlock("a", "Block [Entry]"),
                CreateBlock("b", "Block [Normal]"),
                CreateBlock("c", "Block [Scope]"),
            };

            var restored = BPG_BlockStorage.FromXDocument(BPG_BlockStorage.ToXDocument(originals));

            Assert.That(restored.Select(b => b.id), Is.EqualTo(new[] { "a", "b", "c" }));
            Assert.That(restored.Select(b => b.name), Is.EqualTo(originals.Select(b => b.name)));
        }

        [Test]
        public void XDocument_ブロックが空でも例外にならない() {
            var restored = BPG_BlockStorage.FromXDocument(
                BPG_BlockStorage.ToXDocument(Array.Empty<SerializableBlock>()));

            Assert.That(restored, Is.Empty);
        }

        [Test]
        public void XDocument_ルート名が想定と異なっても読み進められる() {
            // [NOTE] 旧形式のデータを読めるようにするための許容
            var xRoot = new XElement("SomethingElse",
                SerializableBlock.ToXElement(CreateBlock("a", "Block [Normal]")));

            var restored = BPG_BlockStorage.FromXDocument(new XDocument(xRoot));

            Assert.That(restored.Count, Is.EqualTo(1));
            Assert.That(restored[0].id, Is.EqualTo("a"));
        }


        /// ----------------------------------------------------------------------------
        // ファイル入出力

        [Test]
        public void File_保存したデータを読み戻せる() {
            var originals = new[] { CreateBlock("saved", "Block [Normal]") };

            BPG_BlockStorage.SaveSerializableBlocks(_path, originals);
            var restored = BPG_BlockStorage.LoadSerializableBlocks(_path);

            Assert.That(File.Exists(_path), Is.True);
            Assert.That(restored.Count, Is.EqualTo(1));
            Assert.That(restored[0].id, Is.EqualTo("saved"));
        }

        [Test]
        public void File_保存先ディレクトリが存在しなければ作成される() {
            var directory = Path.GetDirectoryName(_path);
            if (Directory.Exists(directory)) {
                Directory.Delete(directory, recursive: true);
            }

            BPG_BlockStorage.SaveSerializableBlocks(_path, new[] { CreateBlock("a", "Block [Normal]") });

            Assert.That(File.Exists(_path), Is.True);
        }

        [Test]
        public void File_存在しないパスなら空のリストを返す() {
            var restored = BPG_BlockStorage.LoadSerializableBlocks(_path);

            Assert.That(restored, Is.Empty);
        }

        [Test]
        public void File_XMLとして壊れていても空のリストを返す() {
            // [NOTE] 保存データはユーザー環境で破損しうるため、例外で停止させない
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            File.WriteAllText(_path, "<BlockPG><Block></BlockPG>");

            var restored = BPG_BlockStorage.LoadSerializableBlocks(_path);

            Assert.That(restored, Is.Empty);
        }

        [Test]
        public void File_Existsとdeleteが状態を正しく反映する() {
            Assert.That(BPG_BlockStorage.Exists(_path), Is.False);
            Assert.That(BPG_BlockStorage.Delete(_path), Is.False);

            BPG_BlockStorage.SaveSerializableBlocks(_path, Array.Empty<SerializableBlock>());

            Assert.That(BPG_BlockStorage.Exists(_path), Is.True);
            Assert.That(BPG_BlockStorage.Delete(_path), Is.True);
            Assert.That(BPG_BlockStorage.Exists(_path), Is.False);
        }


        /// ----------------------------------------------------------------------------
        // パス

        [Test]
        public void GetDefaultPath_拡張子が無ければxmlを補う() {
            var path = BPG_BlockStorage.GetDefaultPath("workspace");

            Assert.That(Path.GetExtension(path), Is.EqualTo(".xml"));
            Assert.That(path, Does.StartWith(Application.persistentDataPath));
        }

        [Test]
        public void GetDefaultPath_拡張子があればそのまま使う() {
            var path = BPG_BlockStorage.GetDefaultPath("workspace.sav");

            Assert.That(Path.GetExtension(path), Is.EqualTo(".sav"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void GetDefaultPath_空のファイル名なら例外を送出する(string fileName) {
            Assert.Throws<ArgumentException>(() => BPG_BlockStorage.GetDefaultPath(fileName));
        }
    }
}
