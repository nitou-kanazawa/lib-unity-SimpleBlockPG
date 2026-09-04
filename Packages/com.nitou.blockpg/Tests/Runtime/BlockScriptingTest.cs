using System.Linq;
using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// スクリプトからのブロック操作を検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] 生成・削除・複製・接続・切断を、ドラッグ操作を介さずに行えることを押さえる．
    /// </remarks>
    public class BlockScriptingTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// 子ブロックを3つ持つ Scope を作る．
        /// </summary>
        private (I_BPG_Block scope, I_BPG_Block a, I_BPG_Block b, I_BPG_Block c) BuildStack() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var body = scope.GetFirstSection().Body;

            var a = _env.CreateBlock(PrefabName.Normal);
            var b = _env.CreateBlock(PrefabName.Normal);
            var c = _env.CreateBlock(PrefabName.Normal);
            body.AppendLast(a);
            body.AppendLast(b);
            body.AppendLast(c);

            scope.Layout.UpdateLayout();
            return (scope, a, b, c);
        }

        private static string[] Order(I_BPG_Block scope) {
            return scope.GetFirstSection().Body.ChildBlocks
                .Select(block => block.Id)
                .ToArray();
        }


        /// ----------------------------------------------------------------------------
        // 生成と削除

        [Test]
        public void 生成したブロックはルートになる() {
            var block = _env.CreateBlock(PrefabName.Normal);

            Assert.That(block.IsRootBlock(), Is.True);
            Assert.That(_env.ProgrammingEnv.GetRootBlocks(), Has.Count.EqualTo(1));
        }

        [Test]
        public void 削除は子孫ごと消える() {
            var (scope, _, _, _) = BuildStack();
            Assert.That(scope.GetAllChildBlocksCount(containSelf: true), Is.EqualTo(4));

            BPG_BlockUtils.RemoveBlock(scope);
            scope.RectTransform.gameObject.SetActive(false);

            Assert.That(_env.ProgrammingEnv.GetRootBlocks(), Is.Empty);
        }


        /// ----------------------------------------------------------------------------
        // 複製

        [Test]
        public void 複製は子孫ごと複製される() {
            var (scope, _, _, _) = BuildStack();

            var copy = BPG_BlockSerializer.Duplicate(scope, _env.ProgrammingEnv);

            Assert.That(copy.GetAllChildBlocksCount(containSelf: true), Is.EqualTo(4));
            Assert.That(copy.Id, Is.Not.EqualTo(scope.Id), "識別IDは振り直されるはず．");
        }


        /// ----------------------------------------------------------------------------
        // 接続

        [Test]
        public void 直後へ挿し込める() {
            var (scope, a, b, c) = BuildStack();
            var x = _env.CreateBlock(PrefabName.Normal);

            Assert.That(x.InsertAfter(a), Is.True);

            Assert.That(Order(scope), Is.EqualTo(new[] { a.Id, x.Id, b.Id, c.Id }));
        }

        [Test]
        public void 直前へ挿し込める() {
            var (scope, a, b, c) = BuildStack();
            var x = _env.CreateBlock(PrefabName.Normal);

            Assert.That(x.InsertBefore(b), Is.True);

            Assert.That(Order(scope), Is.EqualTo(new[] { a.Id, x.Id, b.Id, c.Id }));
        }

        [Test]
        public void 先頭の直前へ挿し込める() {
            var (scope, a, b, c) = BuildStack();
            var x = _env.CreateBlock(PrefabName.Normal);

            x.InsertBefore(a);

            Assert.That(Order(scope), Is.EqualTo(new[] { x.Id, a.Id, b.Id, c.Id }));
        }

        [Test]
        public void ルートブロックの前後には挿せない() {
            // [NOTE] 接続には親セクションが要る．ルートブロック同士は連結できない．
            var root = _env.CreateBlock(PrefabName.Normal);
            var x = _env.CreateBlock(PrefabName.Normal);

            Assert.That(x.InsertAfter(root), Is.False);
            Assert.That(x.InsertBefore(root), Is.False);
            Assert.That(x.IsRootBlock(), Is.True, "失敗しても状態は変わらないはず．");
        }


        /// ----------------------------------------------------------------------------
        // 同じスタック内での移動

        [Test]
        public void 同じスタック内で後ろへ動かせる() {
            // [NOTE] 自分が抜けたぶん後ろが1つ詰まるため、補正しないと1つずれる．
            var (scope, a, b, c) = BuildStack();

            a.InsertAfter(c);

            Assert.That(Order(scope), Is.EqualTo(new[] { b.Id, c.Id, a.Id }));
        }

        [Test]
        public void 同じスタック内で前へ動かせる() {
            var (scope, a, b, c) = BuildStack();

            c.InsertBefore(a);

            Assert.That(Order(scope), Is.EqualTo(new[] { c.Id, a.Id, b.Id }));
        }

        [Test]
        public void 同じスタック内で直前へ動かしても入れ替わる() {
            var (scope, a, b, c) = BuildStack();

            a.InsertBefore(c);

            Assert.That(Order(scope), Is.EqualTo(new[] { b.Id, a.Id, c.Id }));
        }


        /// ----------------------------------------------------------------------------
        // 切断

        [Test]
        public void 切断するとルートになる() {
            var (scope, _, b, _) = BuildStack();

            Assert.That(b.Detach(), Is.True);

            Assert.That(b.IsRootBlock(), Is.True);
            Assert.That(scope.GetFirstSection().Body.ChildBlocks, Has.Count.EqualTo(2));
            Assert.That(_env.ProgrammingEnv.GetRootBlocks(), Has.Count.EqualTo(2));
        }

        [Test]
        public void 切断しても画面上の位置が変わらない() {
            // [NOTE] 切り離しは再ペアレントなので、そのままだと配置先の原点へ飛ぶ．
            var (_, _, b, _) = BuildStack();
            var before = b.RectTransform.position;

            b.Detach();

            Assert.That(b.RectTransform.position.x, Is.EqualTo(before.x).Within(0.01f));
            Assert.That(b.RectTransform.position.y, Is.EqualTo(before.y).Within(0.01f));
        }

        [Test]
        public void 切断すると元の親が縮む() {
            var (scope, _, b, _) = BuildStack();
            float before = scope.RectTransform.sizeDelta.y;

            b.Detach();

            Assert.That(scope.RectTransform.sizeDelta.y, Is.LessThan(before),
                "切り離した直後に親のサイズが詰まっているはず．");
        }

        [Test]
        public void 切断しても後続はスタックに残る() {
            // [NOTE] ルートブロック同士は連結できないため、後続を連れて出ることはできない．
            var (scope, a, b, c) = BuildStack();

            b.Detach();

            Assert.That(Order(scope), Is.EqualTo(new[] { a.Id, c.Id }));
        }

        [Test]
        public void ルートブロックは切断できない() {
            var root = _env.CreateBlock(PrefabName.Normal);

            Assert.That(root.Detach(), Is.False);
        }

        [Test]
        public void 切断したブロックを再接続できる() {
            var (scope, a, b, _) = BuildStack();

            b.Detach();
            Assert.That(b.IsRootBlock(), Is.True, "前提: 切り離されていること");

            Assert.That(b.InsertAfter(a), Is.True);
            Assert.That(b.IsRootBlock(), Is.False);
            Assert.That(scope.GetFirstSection().Body.ChildBlocks, Has.Count.EqualTo(3));
        }

        [Test]
        public void 切断と接続が保存に反映される() {
            var (scope, a, b, c) = BuildStack();
            b.Detach();

            var data = BPG_BlockSerializer.BlockToSerializableBlock(scope);

            Assert.That(data.sections[0].childBlocks, Has.Count.EqualTo(2));
            Assert.That(data.sections[0].childBlocks.Select(x => x.id),
                Is.EqualTo(new[] { a.Id, c.Id }));
        }
    }
}
