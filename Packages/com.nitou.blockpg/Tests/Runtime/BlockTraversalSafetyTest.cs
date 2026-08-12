using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// 階層走査が壊れた状態でも例外を投げないことを検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] 子ブロックのリストは構成変化の検知でしか更新されないため、
    ///        実際の子と食い違う瞬間がありうる．また Body を持たないセクションや
    ///        Layout を持たないブロックも構成上ありえる．
    ///        走査系はそれらを黙って許容し、判定不能なら既定値を返す．
    /// </remarks>
    public class BlockTraversalSafetyTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() {
            _env.Dispose();
            LogAssert.ignoreFailingMessages = false;
        }


        /// ----------------------------------------------------------------------------
        // Body を持たないセクション

        [Test]
        public void Bodyを持たないセクションに属していても例外にならない() {
            // [NOTE] Normal ブロックのセクションは Body を持たない．
            //        その配下に居ることは通常ないが、走査で落ちないことを保証する．
            var parent = _env.CreateBlock(PrefabName.Normal);
            var child = _env.CreateBlock(PrefabName.Normal);

            // Body が無いセクションを親として設定する
            child.SetParentSection(parent.GetFirstSection());

            Assert.That(child.IsFirstBlockInSection(), Is.False);
            Assert.That(child.IsLastBlockInSection(), Is.False);
            Assert.That(child.GetIndexInSection(), Is.EqualTo(-1));
            Assert.That(child.GetPreviousBlock(), Is.Null);
            Assert.That(child.GetNextBlock(), Is.Null);
        }


        /// ----------------------------------------------------------------------------
        // 子リストと実際の子が食い違う状態

        [Test]
        public void 子リストが空でも先頭末尾の判定が例外にならない() {
            // [NOTE] 修正前は First() / Last() で InvalidOperationException になっていた．
            LogAssert.ignoreFailingMessages = true;

            var parent = _env.CreateBlock(PrefabName.Scope);
            var child = _env.CreateBlock(PrefabName.Normal);

            // 親子関係だけ設定し、Transform 上は接続しない（リストは空のまま）
            child.SetParentSection(parent.GetFirstSection());

            Assert.That(child.IsFirstBlockInSection(), Is.False);
            Assert.That(child.IsLastBlockInSection(), Is.False);
            Assert.That(child.GetIndexInSection(), Is.EqualTo(-1));
        }

        [Test]
        public void 接続済みなら先頭末尾を正しく判定する() {
            var parent = _env.CreateBlock(PrefabName.Scope);
            var first = _env.CreateBlock(PrefabName.Normal);
            var last = _env.CreateBlock(PrefabName.Normal);

            var body = parent.GetFirstSection().Body;
            body.AppendLast(first);
            body.AppendLast(last);

            Assert.That(first.IsFirstBlockInSection(), Is.True);
            Assert.That(last.IsLastBlockInSection(), Is.True);
            Assert.That(first.IsLastBlockInSection(), Is.False);
        }


        /// ----------------------------------------------------------------------------
        // Layout を持たないブロック

        /// <summary>
        /// Layout コンポーネントを持たないブロックを組み立てる．
        /// </summary>
        private I_BPG_Block CreateBlockWithoutLayout() {
            var obj = new GameObject("Block [NoLayout]", typeof(RectTransform));
            obj.SetActive(false);
            obj.transform.SetParent(_env.ProgrammingEnv.RectTransform, worldPositionStays: false);
            var block = obj.AddComponent<nitou.BlockPG.Blocks.BPG_Block>();
            obj.SetActive(true);
            return block;
        }

        [Test]
        public void Layoutを搭載していないブロックでも子孫の集計が例外にならない() {
            // [NOTE] 修正前は self.Layout を無条件に参照して NullReferenceException になっていた．
            var block = CreateBlockWithoutLayout();

            Assert.That(block.Layout, Is.Null, "前提: Layout を持たないこと");
            Assert.That(block.GetAllChildBlocksCount(containSelf: true), Is.EqualTo(1));
            Assert.That(block.GetAllChildBlocksCount(containSelf: false), Is.Zero);
            Assert.That(block.GetAllChildBlocks(containSelf: true), Has.Count.EqualTo(1));
            Assert.That(block.GetFirstSection(), Is.Null);
        }

        [Test]
        public void 破棄済みのLayoutでも子孫の集計が例外にならない() {
            // [NOTE] インターフェース型の == は UnityEngine.Object の比較演算子を通らないため、
            //        素朴な null チェックでは破棄済みコンポーネントを弾けない．
            var block = _env.CreateBlock(PrefabName.Scope);
            Object.DestroyImmediate((Object)block.Layout);

            Assert.That((Object)block.Layout == null, Is.True, "前提: Layout が破棄済みであること");
            Assert.That(block.GetAllChildBlocksCount(containSelf: true), Is.EqualTo(1));
            Assert.That(block.GetFirstSection(), Is.Null);
        }

        [Test]
        public void Layoutを持たないブロックの破棄が例外にならない() {
            // [NOTE] RemoveBlock は内部で子孫を辿るため、同じ経路を通る．
            var block = CreateBlockWithoutLayout();

            Assert.DoesNotThrow(() => nitou.BlockPG.Blocks.BPG_BlockUtils.RemoveBlock(block));
        }
    }
}
