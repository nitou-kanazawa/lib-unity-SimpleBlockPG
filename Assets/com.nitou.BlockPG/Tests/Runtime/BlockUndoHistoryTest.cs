using System.Linq;
using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// Undo / Redo を検証する．
    /// </summary>
    public class BlockUndoHistoryTest {

        private BlockPGTestEnv _env;
        private BPG_UndoHistory _history;

        [SetUp]
        public void SetUp() {
            _env = new BlockPGTestEnv();
            _history = new BPG_UndoHistory(_env.ProgrammingEnv);
        }

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // Helper

        private int RootCount => _env.ProgrammingEnv.GetRootBlocks().Count;

        private int TotalCount => _env.ProgrammingEnv.GetRootBlocks()
            .Sum(b => b.GetAllChaildBlocksCount(containSelf: true));


        /// ----------------------------------------------------------------------------
        // 基本動作

        [Test]
        public void 履歴が無ければ取り消せない() {
            Assert.That(_history.CanUndo, Is.False);
            Assert.That(_history.CanRedo, Is.False);
            Assert.That(_history.Undo(), Is.False);
            Assert.That(_history.Redo(), Is.False);
        }

        [Test]
        public void 追加を取り消せる() {
            _history.Record("add");
            _env.CreateBlock(PrefabName.Scope);
            Assert.That(RootCount, Is.EqualTo(1));

            Assert.That(_history.Undo(), Is.True);
            Assert.That(RootCount, Is.Zero);
        }

        [Test]
        public void 取り消しをやり直せる() {
            _history.Record("add");
            _env.CreateBlock(PrefabName.Scope);
            _history.Undo();

            Assert.That(_history.Redo(), Is.True);
            Assert.That(RootCount, Is.EqualTo(1));
        }

        [Test]
        public void 削除を取り消せる() {
            var block = _env.CreateBlock(PrefabName.Scope);

            _history.Record("remove");
            _env.ProgrammingEnv.RemoveAllBlocks();
            Assert.That(RootCount, Is.Zero);

            _history.Undo();
            Assert.That(RootCount, Is.EqualTo(1));
        }

        [Test]
        public void 接続を取り消せる() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var normal = _env.CreateBlock(PrefabName.Normal);
            Assert.That(RootCount, Is.EqualTo(2), "前提: どちらもルートであること");

            _history.Record("connect");
            scope.GetFirstSection().Body.AppendLast(normal);
            Assert.That(RootCount, Is.EqualTo(1));

            _history.Undo();
            Assert.That(RootCount, Is.EqualTo(2), "接続前の状態へ戻っていない");
        }


        /// ----------------------------------------------------------------------------
        // 状態の保持

        [Test]
        public void 識別IDが取り消しをまたいで維持される() {
            // [NOTE] 復元でインスタンスは作り直されるため、参照ではなくIDで追える必要がある．
            var block = _env.CreateBlock(PrefabName.Scope);
            var id = block.Id;

            _history.Record("add");
            _env.CreateBlock(PrefabName.Normal);
            _history.Undo();

            var found = _env.ProgrammingEnv.FindBlockById(id);
            Assert.That(found, Is.Not.Null, "IDからブロックを引けない");
            Assert.That(found.Id, Is.EqualTo(id));
        }

        [Test]
        public void 入れ子構造が取り消しで復元される() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var body = scope.GetFirstSection().Body;
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            Assert.That(TotalCount, Is.EqualTo(3));

            _history.Record("clear");
            _env.ProgrammingEnv.RemoveAllBlocks();
            _history.Undo();

            Assert.That(TotalCount, Is.EqualTo(3));
            Assert.That(_env.ProgrammingEnv.GetRootBlocks()[0]
                .GetFirstSection().Body.ChildBlocks, Has.Count.EqualTo(2));
        }

        [Test]
        public void 折り畳み状態が取り消しで復元される() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            scope.GetFirstSection().Body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            scope.GetFirstSection().SetCollapsed(true);

            _history.Record("clear");
            _env.ProgrammingEnv.RemoveAllBlocks();
            _history.Undo();

            Assert.That(_env.ProgrammingEnv.GetRootBlocks()[0]
                .GetFirstSection().IsCollapsed, Is.True);
        }

        [Test]
        public void 復元はフレームをまたがずに完了する() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            scope.GetFirstSection().Body.AppendLast(_env.CreateBlock(PrefabName.Normal));

            _history.Record("clear");
            _env.ProgrammingEnv.RemoveAllBlocks();

            int frameBefore = Time.frameCount;
            _history.Undo();

            Assert.That(Time.frameCount, Is.EqualTo(frameBefore));
            Assert.That(TotalCount, Is.EqualTo(2), "戻り時点で子孫まで組み上がっていない");
        }


        /// ----------------------------------------------------------------------------
        // 履歴の管理

        [Test]
        public void 新しい記録でやり直し履歴が破棄される() {
            _history.Record("first");
            _env.CreateBlock(PrefabName.Scope);
            _history.Undo();
            Assert.That(_history.CanRedo, Is.True);

            _history.Record("second");
            Assert.That(_history.CanRedo, Is.False);
        }

        [Test]
        public void 複数回の取り消しとやり直しが順に進む() {
            _history.Record("1");
            _env.CreateBlock(PrefabName.Scope);
            _history.Record("2");
            _env.CreateBlock(PrefabName.Normal);
            Assert.That(RootCount, Is.EqualTo(2));

            _history.Undo();
            Assert.That(RootCount, Is.EqualTo(1));
            _history.Undo();
            Assert.That(RootCount, Is.Zero);

            _history.Redo();
            Assert.That(RootCount, Is.EqualTo(1));
            _history.Redo();
            Assert.That(RootCount, Is.EqualTo(2));
        }

        [Test]
        public void 上限を超えた古い履歴は捨てられる() {
            var history = new BPG_UndoHistory(_env.ProgrammingEnv, capacity: 3);
            for (int i = 0; i < 5; i++) {
                history.Record($"op{i}");
                _env.CreateBlock(PrefabName.Normal);
            }

            Assert.That(history.UndoCount, Is.EqualTo(3));
        }

        [Test]
        public void 操作の名前を取得できる() {
            _history.Record("ブロックの追加");
            _env.CreateBlock(PrefabName.Scope);

            Assert.That(_history.NextUndoLabel, Is.EqualTo("ブロックの追加"));

            _history.Undo();
            Assert.That(_history.NextRedoLabel, Is.EqualTo("ブロックの追加"));
        }

        [Test]
        public void 復元時に通知が飛ぶ() {
            string notified = null;
            _history.OnRestored += label => notified = label;

            _history.Record("add");
            _env.CreateBlock(PrefabName.Scope);
            _history.Undo();

            Assert.That(notified, Is.EqualTo("add"));
        }

        [Test]
        public void 履歴を破棄できる() {
            _history.Record("add");
            _env.CreateBlock(PrefabName.Scope);
            _history.Undo();

            _history.Clear();

            Assert.That(_history.CanUndo, Is.False);
            Assert.That(_history.CanRedo, Is.False);
        }
    }
}
