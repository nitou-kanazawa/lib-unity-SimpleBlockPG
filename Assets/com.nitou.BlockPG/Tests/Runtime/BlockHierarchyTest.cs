using System.Linq;
using NUnit.Framework;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ブロック階層の走査に関するテスト．
    /// </summary>
    public class BlockHierarchyTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // 生成直後の状態

        [Test]
        public void 生成直後のブロックはルートブロックになる() {
            var block = _env.CreateBlock(PrefabName.Normal);

            Assert.That(block.IsRootBlock(), Is.True);
            Assert.That(block.ParentSection, Is.Null);
            Assert.That(block.GetParentBlock(), Is.Null);
            Assert.That(block.GetRootBlock(), Is.EqualTo(block));
        }

        [Test]
        public void 生成直後にLayoutとSectionsが解決済みになる() {
            // [NOTE] Instantiate() が Awake() を同期的に完了させることに依存している．
            //        この前提が崩れると復元処理の1フレーム完結も成立しなくなる．
            var block = _env.CreateBlock(PrefabName.Scope);

            Assert.That(block.Layout, Is.Not.Null);
            Assert.That(block.Layout.Sections, Is.Not.Empty);
            Assert.That(block.GetFirstSection().Body, Is.Not.Null);
        }

        [Test]
        public void 識別IDは生成ごとに一意になる() {
            var first = _env.CreateBlock(PrefabName.Normal);
            var second = _env.CreateBlock(PrefabName.Normal);

            Assert.That(first.Id, Is.Not.Empty);
            Assert.That(first.Id, Is.Not.EqualTo(second.Id));
        }


        /// ----------------------------------------------------------------------------
        // 接続後の走査

        [Test]
        public void 接続すると親子関係を辿れる() {
            var parent = _env.CreateBlock(PrefabName.Scope);
            var child = _env.CreateBlock(PrefabName.Normal);

            parent.GetFirstSection().Body.AppendLast(child);

            Assert.That(child.IsRootBlock(), Is.False);
            Assert.That(child.GetParentBlock(), Is.EqualTo(parent));
            Assert.That(child.GetRootBlock(), Is.EqualTo(parent));
        }

        [Test]
        public void 同じ親を持つ前後のブロックを取得できる() {
            var parent = _env.CreateBlock(PrefabName.Scope);
            var first = _env.CreateBlock(PrefabName.Normal);
            var second = _env.CreateBlock(PrefabName.Normal);
            var third = _env.CreateBlock(PrefabName.Normal);

            var body = parent.GetFirstSection().Body;
            body.AppendLast(first);
            body.AppendLast(second);
            body.AppendLast(third);

            Assert.That(second.GetPreviousBlock(), Is.EqualTo(first));
            Assert.That(second.GetNextBlock(), Is.EqualTo(third));
            Assert.That(first.GetPreviousBlock(), Is.Null);
            Assert.That(third.GetNextBlock(), Is.Null);
        }

        [Test]
        public void セクション内のインデックスと先頭末尾を判定できる() {
            var parent = _env.CreateBlock(PrefabName.Scope);
            var first = _env.CreateBlock(PrefabName.Normal);
            var last = _env.CreateBlock(PrefabName.Normal);

            var body = parent.GetFirstSection().Body;
            body.AppendLast(first);
            body.AppendLast(last);

            Assert.That(first.GetIndexInSection(), Is.EqualTo(0));
            Assert.That(last.GetIndexInSection(), Is.EqualTo(1));
            Assert.That(first.IsFirstBlockInSection(), Is.True);
            Assert.That(last.IsLastBlockInSection(), Is.True);
            Assert.That(first.IsLastBlockInSection(), Is.False);
        }

        [Test]
        public void ルートブロックは前後もインデックスも持たない() {
            var block = _env.CreateBlock(PrefabName.Normal);

            Assert.That(block.GetPreviousBlock(), Is.Null);
            Assert.That(block.GetNextBlock(), Is.Null);
            Assert.That(block.GetIndexInSection(), Is.EqualTo(-1));
            Assert.That(block.IsFirstBlockInSection(), Is.False);
            Assert.That(block.IsLastBlockInSection(), Is.False);
        }


        /// ----------------------------------------------------------------------------
        // 子孫の集計

        [Test]
        public void 入れ子になった子孫をすべて取得できる() {
            var root = _env.CreateBlock(PrefabName.Scope);
            var middle = _env.CreateBlock(PrefabName.Scope);
            var leaf = _env.CreateBlock(PrefabName.Normal);

            root.GetFirstSection().Body.AppendLast(middle);
            middle.GetFirstSection().Body.AppendLast(leaf);

            Assert.That(root.GetAllChaildBlocksCount(containSelf: true), Is.EqualTo(3));
            Assert.That(root.GetAllChaildBlocksCount(containSelf: false), Is.EqualTo(2));
            Assert.That(root.GetAllChaildBlocks(containSelf: true), Is.EquivalentTo(new[] { root, middle, leaf }));
        }

        [Test]
        public void ブロックを破棄すると子孫ごと取り除かれる() {
            var root = _env.CreateBlock(PrefabName.Scope);
            var child = _env.CreateBlock(PrefabName.Normal);
            root.GetFirstSection().Body.AppendLast(child);

            BPG_BlockUtils.RemoveBlock(root);
            // ※Destroy はフレーム終端まで遅延するため、テスト内では即時破棄で確認する
            UnityEngine.Object.DestroyImmediate(root.RectTransform.gameObject);

            Assert.That(_env.GetRootBlocks(), Is.Empty);
        }
    }
}
