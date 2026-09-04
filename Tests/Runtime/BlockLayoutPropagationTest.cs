using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// レイアウト更新が入れ子のブロックまで届くことを検証する．
    /// [NOTE] レイアウト更新はルートブロックからの一度の再帰で部分木全体を揃える設計になっている．
    ///        子ブロック自身の LateUpdate は親を持つ場合に早期リターンするため、
    ///        再帰が途中で止まると入れ子のブロックが永久に更新されなくなる．
    /// </summary>
    public class BlockLayoutPropagationTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------

        [Test]
        public void ルートの更新が子ブロックのレイアウトに届く() {
            // Arrange
            var root = _env.CreateBlock(PrefabName.Scope);
            var child = _env.CreateBlock(PrefabName.Scope);
            root.GetFirstSection().Body.AppendLast(child);

            // 子のサイズを崩しておく
            child.RectTransform.sizeDelta = Vector2.zero;

            // Act : ルートからのみ更新する
            root.Layout.UpdateLayout();

            // Assert : 子のサイズが再計算されている
            Assert.That(child.RectTransform.sizeDelta, Is.EqualTo(child.Layout.Size));
            Assert.That(child.RectTransform.sizeDelta, Is.Not.EqualTo(Vector2.zero));
        }

        [Test]
        public void ルートの更新が孫ブロックまで届く() {
            var root = _env.CreateBlock(PrefabName.Scope);
            var child = _env.CreateBlock(PrefabName.Scope);
            var grandChild = _env.CreateBlock(PrefabName.Scope);

            root.GetFirstSection().Body.AppendLast(child);
            child.GetFirstSection().Body.AppendLast(grandChild);

            grandChild.RectTransform.sizeDelta = Vector2.zero;

            root.Layout.UpdateLayout();

            Assert.That(grandChild.RectTransform.sizeDelta, Is.EqualTo(grandChild.Layout.Size));
            Assert.That(grandChild.RectTransform.sizeDelta, Is.Not.EqualTo(Vector2.zero));
        }

        [Test]
        public void 複数セクションそれぞれの子へ届く() {
            var root = _env.CreateBlock(PrefabName.MultiScope);
            var first = _env.CreateBlock(PrefabName.Scope);
            var second = _env.CreateBlock(PrefabName.Scope);

            var sections = root.Layout.Sections;
            sections[0].Body.AppendLast(first);
            sections[1].Body.AppendLast(second);

            first.RectTransform.sizeDelta = Vector2.zero;
            second.RectTransform.sizeDelta = Vector2.zero;

            root.Layout.UpdateLayout();

            Assert.That(first.RectTransform.sizeDelta, Is.Not.EqualTo(Vector2.zero));
            Assert.That(second.RectTransform.sizeDelta, Is.Not.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator dirtyフラグ経由でも子ブロックが更新される() {
            // [NOTE] 実際の更新経路（SetLayoutDirty -> ルートの LateUpdate）でも届くことを確認する．
            var root = _env.CreateBlock(PrefabName.Scope);
            var child = _env.CreateBlock(PrefabName.Scope);
            root.GetFirstSection().Body.AppendLast(child);

            yield return null;

            child.RectTransform.sizeDelta = Vector2.zero;
            child.Layout.SetLayoutDirty();

            yield return null;

            Assert.That(child.RectTransform.sizeDelta, Is.Not.EqualTo(Vector2.zero));
        }
    }
}
