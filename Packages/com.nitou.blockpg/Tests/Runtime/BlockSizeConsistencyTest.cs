using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ブロックの実サイズ（RectTransform.sizeDelta）と、計算上のサイズ（Layout.Size）が
    /// 一致することを検証する．
    /// [NOTE] 親のセクションは子の Layout.Size を合計して高さを決める一方、
    ///        実際の配置は LayoutGroup が子の sizeDelta を使って行う．
    ///        両者がずれると、確保した高さと描画内容が食い違って隙間になる．
    /// </summary>
    public class BlockSizeConsistencyTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// 部分木のすべてのブロックについて、実サイズと計算上のサイズの一致を検証する．
        /// </summary>
        private static void AssertSizeIsConsistent(I_BPG_Block root) {
            foreach (var block in root.GetAllChildBlocks(containSelf: true)) {
                Assert.That(block.RectTransform.sizeDelta, Is.EqualTo(block.Layout.Size),
                    $"'{block.RectTransform.name}' の実サイズが計算値と一致していない．");
            }
        }


        /// ----------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator 入れ子のScopeへ子を追加してもサイズが一致する() {
            // Arrange : Entry > Scope の状態を作って落ち着かせる
            var entry = _env.CreateBlock(PrefabName.Entry);
            var scope = _env.CreateBlock(PrefabName.Scope);
            entry.GetFirstSection().Body.AppendLast(scope);
            yield return null;

            AssertSizeIsConsistent(entry);

            // Act : 配置済みの Scope の中へブロックを追加する
            var normal = _env.CreateBlock(PrefabName.Normal);
            scope.GetFirstSection().Body.AppendLast(normal);
            yield return null;

            // Assert
            AssertSizeIsConsistent(entry);
        }

        [UnityTest]
        public IEnumerator 入れ子のMultiScopeへ子を追加してもサイズが一致する() {
            var entry = _env.CreateBlock(PrefabName.Entry);
            var multi = _env.CreateBlock(PrefabName.MultiScope);
            entry.GetFirstSection().Body.AppendLast(multi);
            yield return null;

            var first = _env.CreateBlock(PrefabName.Normal);
            multi.Layout.Sections[0].Body.AppendLast(first);
            yield return null;

            AssertSizeIsConsistent(entry);

            var second = _env.CreateBlock(PrefabName.Normal);
            multi.Layout.Sections[1].Body.AppendLast(second);
            yield return null;

            AssertSizeIsConsistent(entry);
        }

        [UnityTest]
        public IEnumerator 組み立て済みのScopeを配置した場合もサイズが一致する() {
            // [NOTE] こちらは不具合が出ない経路．修正で壊れていないことの確認用．
            var scope = _env.CreateBlock(PrefabName.Scope);
            var normal = _env.CreateBlock(PrefabName.Normal);
            scope.GetFirstSection().Body.AppendLast(normal);
            yield return null;

            var entry = _env.CreateBlock(PrefabName.Entry);
            entry.GetFirstSection().Body.AppendLast(scope);
            yield return null;

            AssertSizeIsConsistent(entry);
        }

        [Test]
        public void UpdateLayoutは一度の呼び出しでサイズを確定させる() {
            // [NOTE] dirtyフラグは更新後にクリアされるため、一度の走査で確定しないと
            //        ずれたまま次の変更まで残り続ける．
            var entry = _env.CreateBlock(PrefabName.Entry);
            var scope = _env.CreateBlock(PrefabName.Scope);
            var normal = _env.CreateBlock(PrefabName.Normal);

            entry.GetFirstSection().Body.AppendLast(scope);
            scope.GetFirstSection().Body.AppendLast(normal);

            entry.Layout.UpdateLayout();

            AssertSizeIsConsistent(entry);
        }

        [UnityTest]
        public IEnumerator 親セクションの高さが子ブロックの実サイズの合計と対応する() {
            // [NOTE] 隙間として見える現象を直接検証する．
            //        セクションの高さは子の Layout.Size から算出されるため、
            //        LayoutGroup が使う実サイズと乖離していると余白になる．
            var entry = _env.CreateBlock(PrefabName.Entry);
            var scope = _env.CreateBlock(PrefabName.Scope);
            entry.GetFirstSection().Body.AppendLast(scope);
            yield return null;

            var normal = _env.CreateBlock(PrefabName.Normal);
            scope.GetFirstSection().Body.AppendLast(normal);
            yield return null;

            var body = entry.GetFirstSection().Body;
            float sumOfActual = body.ChildBlocks.Sum(c => c.RectTransform.sizeDelta.y);
            float sumOfComputed = body.ChildBlocks.Sum(c => c.Layout.Size.y);

            Assert.That(sumOfActual, Is.EqualTo(sumOfComputed).Within(0.01f),
                "子ブロックの実サイズ合計と計算値合計がずれている．差分がそのまま隙間になる．");
        }
    }
}
