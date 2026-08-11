using System.Linq;
using NUnit.Framework;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ブロックの複製を検証する．
    /// </summary>
    public class BlockDuplicateTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------

        [Test]
        public void 複製すると同じ構成のブロックが増える() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var body = scope.GetFirstSection().Body;
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));

            var clone = BPG_BlockSerializer.Duplicate(scope, _env.ProgrammingEnv);

            Assert.That(clone, Is.Not.Null);
            Assert.That(clone.RectTransform.name, Is.EqualTo(scope.RectTransform.name));
            Assert.That(clone.GetAllChildBlocksCount(containSelf: true),
                Is.EqualTo(scope.GetAllChildBlocksCount(containSelf: true)));
        }

        [Test]
        public void 複製は元とは別のインスタンスになる() {
            var block = _env.CreateBlock(PrefabName.Scope);

            var clone = BPG_BlockSerializer.Duplicate(block, _env.ProgrammingEnv);

            Assert.That(clone, Is.Not.EqualTo(block));
            Assert.That(_env.ProgrammingEnv.GetRootBlocks(), Has.Count.EqualTo(2));
        }

        [Test]
        public void 複製には新しい識別IDが振られる() {
            // [NOTE] 同じIDのブロックが2つ存在すると、IDによる参照が壊れる．
            var scope = _env.CreateBlock(PrefabName.Scope);
            scope.GetFirstSection().Body.AppendLast(_env.CreateBlock(PrefabName.Normal));

            var clone = BPG_BlockSerializer.Duplicate(scope, _env.ProgrammingEnv);

            var originalIds = scope.GetAllChildBlocks(containSelf: true).Select(b => b.Id).ToArray();
            var cloneIds = clone.GetAllChildBlocks(containSelf: true).Select(b => b.Id).ToArray();

            Assert.That(cloneIds, Is.All.Not.Empty);
            Assert.That(cloneIds.Intersect(originalIds), Is.Empty, "元と同じIDが使い回されている．");
        }

        [Test]
        public void 複製した全ブロックのIDが一意になる() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var body = scope.GetFirstSection().Body;
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));

            var clone = BPG_BlockSerializer.Duplicate(scope, _env.ProgrammingEnv);

            var ids = clone.GetAllChildBlocks(containSelf: true).Select(b => b.Id).ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Length));
        }

        [Test]
        public void 複製はルートブロックとして配置される() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var nested = _env.CreateBlock(PrefabName.Normal);
            scope.GetFirstSection().Body.AppendLast(nested);

            // 入れ子のブロックを複製する
            var clone = BPG_BlockSerializer.Duplicate(nested, _env.ProgrammingEnv);

            Assert.That(clone.IsRootBlock(), Is.True);
        }

        [Test]
        public void 折り畳み状態も複製される() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            scope.GetFirstSection().Body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            scope.GetFirstSection().SetCollapsed(true);

            var clone = BPG_BlockSerializer.Duplicate(scope, _env.ProgrammingEnv);

            Assert.That(clone.GetFirstSection().IsCollapsed, Is.True);
        }

        [Test]
        public void 複製後もIDでそれぞれを引ける() {
            var block = _env.CreateBlock(PrefabName.Scope);
            var clone = BPG_BlockSerializer.Duplicate(block, _env.ProgrammingEnv);

            Assert.That(_env.ProgrammingEnv.FindBlockById(block.Id), Is.EqualTo(block));
            Assert.That(_env.ProgrammingEnv.FindBlockById(clone.Id), Is.EqualTo(clone));
        }
    }
}
