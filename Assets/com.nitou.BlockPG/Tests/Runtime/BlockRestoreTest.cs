using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// セーブデータからのブロック復元に関するテスト．
    /// </summary>
    public class BlockRestoreTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() {
            _env.Dispose();
            LogAssert.ignoreFailingMessages = false;
        }


        /// ----------------------------------------------------------------------------
        // Helper

        private static SerializableBlock Block(string prefabName, string id = null) {
            return new SerializableBlock(id ?? prefabName, prefabName, Vector3.zero);
        }

        /// <summary>
        /// 親ブロックの指定セクションへ子ブロックを登録する．
        /// </summary>
        private static SerializableBlock WithChildren(SerializableBlock parent, params SerializableBlock[] children) {
            var section = new SerializableBlockSection();
            section.childBlocks.AddRange(children);
            parent.sections.Add(section);
            return parent;
        }


        /// ----------------------------------------------------------------------------
        // 1フレーム完結の検証

        [Test]
        public void 復元はフレームをまたがずに完了する() {
            // Arrange : 3階層の入れ子
            var data = WithChildren(Block(PrefabName.Scope, "root"),
                WithChildren(Block(PrefabName.Scope, "middle"),
                    Block(PrefabName.Normal, "leaf")));

            // Act
            var frameBefore = Time.frameCount;
            var block = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);
            var frameAfter = Time.frameCount;

            // Assert : フレームが進んでいない
            Assert.That(frameAfter, Is.EqualTo(frameBefore),
                "復元処理がフレームをまたいでいる．");

            // Assert : 戻り値の時点で子孫まで組み上がっている（yieldを挟まずに検証する）
            Assert.That(block, Is.Not.Null);
            var all = block.GetAllChaildBlocks(containSelf: true);
            Assert.That(all.Count, Is.EqualTo(3), "子孫ブロックが生成されていない．");
            Assert.That(all.Select(b => b.Id), Is.EquivalentTo(new[] { "root", "middle", "leaf" }));
        }

        [UnityTest]
        public IEnumerator 復元直後と次フレームで階層が変化しない() {
            // [NOTE] 以前の実装は階層ごとに1フレーム待って子を足していたため、
            //        フレームをまたぐと構成が増えていた．その退行を検出する．
            var data = WithChildren(Block(PrefabName.Scope, "root"),
                WithChildren(Block(PrefabName.Scope, "middle"),
                    Block(PrefabName.Normal, "leaf")));

            var block = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);
            var countAtRestore = block.GetAllChaildBlocksCount(containSelf: true);

            yield return null;
            yield return null;

            Assert.That(block.GetAllChaildBlocksCount(containSelf: true), Is.EqualTo(countAtRestore));
            Assert.That(countAtRestore, Is.EqualTo(3));
        }


        /// ----------------------------------------------------------------------------
        // 構造の復元

        [Test]
        public void 識別IDが保存時の値で復元される() {
            var block = BPG_BlockSerializer.SerializableBlockToBlock(
                Block(PrefabName.Normal, "saved-id"), _env.ProgrammingEnv);

            Assert.That(block.Id, Is.EqualTo("saved-id"));
        }

        [Test]
        public void 子ブロックの並び順が保存時のまま復元される() {
            // [NOTE] 以前は先頭へ挿入していたため順序が反転していた．
            var data = WithChildren(Block(PrefabName.Scope, "root"),
                Block(PrefabName.Normal, "first"),
                Block(PrefabName.Normal, "second"),
                Block(PrefabName.Normal, "third"));

            var block = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);

            var childIds = block.GetFirstSection().Body.ChildBlocks.Select(b => b.Id);
            Assert.That(childIds, Is.EqualTo(new[] { "first", "second", "third" }));
        }

        [Test]
        public void 複数セクションがそれぞれ独立して復元される() {
            var data = Block(PrefabName.MultiScope, "root");
            WithChildren(data, Block(PrefabName.Normal, "inFirst"));
            WithChildren(data, Block(PrefabName.Normal, "inSecond"));

            var block = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);

            var sections = block.Layout.Sections;
            Assert.That(sections.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(sections[0].Body.ChildBlocks.Single().Id, Is.EqualTo("inFirst"));
            Assert.That(sections[1].Body.ChildBlocks.Single().Id, Is.EqualTo("inSecond"));
        }

        [Test]
        public void 親子関係が双方向に設定される() {
            var data = WithChildren(Block(PrefabName.Scope, "root"), Block(PrefabName.Normal, "child"));

            var block = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);
            var child = block.GetFirstSection().Body.ChildBlocks.Single();

            Assert.That(child.GetParentBlock(), Is.EqualTo(block));
            Assert.That(child.GetRootBlock(), Is.EqualTo(block));
            Assert.That(block.IsRootBlock(), Is.True);
        }


        /// ----------------------------------------------------------------------------
        // 異常系

        [Test]
        public void 存在しないプレハブ名ならnullを返す() {
            LogAssert.ignoreFailingMessages = true;

            var block = BPG_BlockSerializer.SerializableBlockToBlock(
                Block("Block [DoesNotExist]"), _env.ProgrammingEnv);

            Assert.That(block, Is.Null);
        }

        [Test]
        public void 子ブロックのプレハブが欠けても残りは復元される() {
            LogAssert.ignoreFailingMessages = true;

            var data = WithChildren(Block(PrefabName.Scope, "root"),
                Block(PrefabName.Normal, "ok1"),
                Block("Block [DoesNotExist]", "missing"),
                Block(PrefabName.Normal, "ok2"));

            var block = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);

            var childIds = block.GetFirstSection().Body.ChildBlocks.Select(b => b.Id);
            Assert.That(childIds, Is.EqualTo(new[] { "ok1", "ok2" }));
        }

        [Test]
        public void セクション数が保存時と食い違っても処理できる範囲で復元する() {
            LogAssert.ignoreFailingMessages = true;

            // Arrange : 1セクションしか持たないプレハブに2セクション分のデータを与える
            var data = Block(PrefabName.Scope, "root");
            WithChildren(data, Block(PrefabName.Normal, "inFirst"));
            WithChildren(data, Block(PrefabName.Normal, "inSecond"));

            // Act
            var block = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);

            // Assert : 1つ目のセクションだけ復元される
            Assert.That(block, Is.Not.Null);
            Assert.That(block.GetFirstSection().Body.ChildBlocks.Single().Id, Is.EqualTo("inFirst"));
        }
    }
}
