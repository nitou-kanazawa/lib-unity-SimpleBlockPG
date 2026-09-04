using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;
using nitou.BlockPG.Blocks.Section;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ヘッダーの入力値とブロック固有データの保存・復元を検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] どちらも「器はあるが変換処理がコメントアウトされている」状態だった．
    ///        入力を実装した時点で「保存して読み込んだら入力値だけ消える」形で表面化するため、
    ///        通しの往復をテストで押さえる．
    /// </remarks>
    public class BlockInputSerializationTest {

        private BlockPGTestEnv _env;
        private string _path;

        [SetUp]
        public void SetUp() {
            _env = new BlockPGTestEnv();
            _path = Path.Combine(Application.temporaryCachePath, "BlockPGTests", $"{Guid.NewGuid():N}.xml");
        }

        [TearDown]
        public void TearDown() {
            _env.Dispose();
            if (File.Exists(_path)) File.Delete(_path);
        }


        /// ----------------------------------------------------------------------------
        // Helper

        private static I_BPG_BlockSectionHeaderInput FirstInput(I_BPG_Block block) {
            return block.GetFirstSection().Header.Inputs[0];
        }

        private static TestBlockCustomData CustomDataOf(I_BPG_Block block) {
            return block.RectTransform.GetComponent<TestBlockCustomData>();
        }


        /// ----------------------------------------------------------------------------
        // 収集

        [Test]
        public void ヘッダーが入力要素を列挙する() {
            var block = _env.CreateBlock(PrefabName.TestInput);
            var header = block.GetFirstSection().Header;

            Assert.That(header.Inputs, Has.Count.EqualTo(1));
            Assert.That(header.Items, Has.Count.GreaterThan(header.Inputs.Count),
                "入力以外のアイテムも Items には含まれるはず．");
        }

        [Test]
        public void 入力を持たないブロックでは空になる() {
            var block = _env.CreateBlock(PrefabName.Normal);

            Assert.That(block.GetFirstSection().Header.Inputs, Is.Empty);
        }


        /// ----------------------------------------------------------------------------
        // 入力値の保存と復元

        [Test]
        public void 入力値が保存と復元で維持される() {
            var block = _env.CreateBlock(PrefabName.TestInput);
            FirstInput(block).SetValue("こんにちは");

            BPG_BlockStorage.Save(_path, new[] { block });
            UnityEngine.Object.DestroyImmediate(block.RectTransform.gameObject);

            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            Assert.That(restored, Has.Count.EqualTo(1));
            Assert.That(FirstInput(restored[0]).Value, Is.EqualTo("こんにちは"),
                "保存して読み込むと入力値が失われている．");
        }

        [Test]
        public void 入れ子のブロックの入力値も維持される() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var child = _env.CreateBlock(PrefabName.TestInput);
            scope.GetFirstSection().Body.AppendLast(child);
            FirstInput(child).SetValue("nested");

            BPG_BlockStorage.Save(_path, new[] { scope });
            UnityEngine.Object.DestroyImmediate(scope.RectTransform.gameObject);

            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);
            var restoredChild = restored[0].GetFirstSection().Body.ChildBlocks[0];

            Assert.That(FirstInput(restoredChild).Value, Is.EqualTo("nested"));
        }

        [Test]
        public void 複製でも入力値が引き継がれる() {
            var block = _env.CreateBlock(PrefabName.TestInput);
            FirstInput(block).SetValue("copy me");

            var copy = BPG_BlockSerializer.Duplicate(block, _env.ProgrammingEnv);

            Assert.That(FirstInput(copy).Value, Is.EqualTo("copy me"));
            Assert.That(copy.Id, Is.Not.EqualTo(block.Id), "前提: 識別IDは振り直される");
        }

        [Test]
        public void 入力対応前のデータでも読める() {
            // [NOTE] inputs 要素が無い旧データとの互換．
            var data = new SerializableBlock("root", PrefabName.TestInput, Vector3.zero);
            data.sections.Add(new SerializableBlockSection());

            var xml = SerializableBlock.ToXElement(data);
            foreach (var stale in System.Linq.Enumerable.ToArray(xml.Descendants("inputs"))) {
                stale.Remove();
            }

            LogAssert.Expect(LogType.Warning, new Regex("Input count does not match"));

            var restored = BPG_BlockSerializer.XmlToBlock(xml, _env.ProgrammingEnv);
            Assert.That(restored, Is.Not.Null);
            Assert.That(FirstInput(restored).Value, Is.Empty, "既定値のまま復元されるはず．");
        }

        [Test]
        public void 入力数が食い違うと警告する() {
            // [NOTE] プレハブ側の入力構成が保存時から変わった場合を模した状況．
            var data = new SerializableBlock("root", PrefabName.TestInput, Vector3.zero);
            var section = new SerializableBlockSection();
            section.inputs.Add(new SerializableInput("a"));
            section.inputs.Add(new SerializableInput("b"));   // ※プレハブ側には1つしか無い
            data.sections.Add(section);

            LogAssert.Expect(LogType.Warning, new Regex("Input count does not match"));

            var restored = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);

            Assert.That(FirstInput(restored).Value, Is.EqualTo("a"),
                "処理できる範囲は復元されるはず．");
        }


        /// ----------------------------------------------------------------------------
        // ブロック固有データ

        [Test]
        public void ブロック固有データが保存と復元で維持される() {
            var block = _env.CreateBlock(PrefabName.TestInput);
            CustomDataOf(block).Data = "{\"hp\":10}";

            BPG_BlockStorage.Save(_path, new[] { block });
            UnityEngine.Object.DestroyImmediate(block.RectTransform.gameObject);

            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            Assert.That(CustomDataOf(restored[0]).Data, Is.EqualTo("{\"hp\":10}"),
                "ブロック固有データが失われている．");
        }

        [Test]
        public void 固有データを持たないブロックでも保存できる() {
            var block = _env.CreateBlock(PrefabName.Normal);

            BPG_BlockStorage.Save(_path, new[] { block });
            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            Assert.That(restored, Has.Count.EqualTo(1));
        }

        [Test]
        public void 受け手が居ないのに固有データがあると警告する() {
            // [NOTE] プレハブ構成の変更で受け手が消えた場合．黙って捨てると原因を追えない．
            var data = new SerializableBlock("root", PrefabName.Normal, Vector3.zero);
            data.customData = "orphan";

            LogAssert.Expect(LogType.Warning, new Regex("no receiver is attached"));

            var restored = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);
            Assert.That(restored, Is.Not.Null, "警告は出しても復元自体は継続するはず．");
        }


        /// ----------------------------------------------------------------------------
        // 取り消し

        [Test]
        public void 入力値が取り消しで復元される() {
            // [NOTE] Undo はスナップショット方式でシリアライザを通すため、同じ穴の影響を受ける．
            var history = new BPG_UndoHistory(_env.ProgrammingEnv);
            var block = _env.CreateBlock(PrefabName.TestInput);
            FirstInput(block).SetValue("before");

            history.Record("clear");
            _env.ProgrammingEnv.RemoveAllBlocks();
            history.Undo();

            var restored = _env.ProgrammingEnv.GetRootBlocks()[0];
            Assert.That(FirstInput(restored).Value, Is.EqualTo("before"));
        }
    }
}
