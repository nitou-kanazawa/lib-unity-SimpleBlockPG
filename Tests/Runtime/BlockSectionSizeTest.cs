using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// セクションのサイズ計算を検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] 「最初／最後のセクションか」の判定は、以前は兄弟インデックスで行っていた．
    ///        ブロック直下の最後の子が OuterArea であることに暗黙に依存しており、
    ///        プレハブに子を1つ足すだけで壊れる状態だった．この性質をテストで押さえる．
    /// </remarks>
    public class BlockSectionSizeTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// ブロック直下に、セクションでもOuterAreaでもない子を末尾へ足す．
        /// </summary>
        private static void AppendDecorationChild(I_BPG_Block block) {
            var go = new GameObject("Decoration", typeof(RectTransform));
            go.transform.SetParent(block.RectTransform, false);
            go.transform.SetAsLastSibling();
        }


        /// ----------------------------------------------------------------------------
        // 兄弟インデックスへの非依存

        [Test]
        public void ブロック直下に子を足しても最後のセクションの長さが変わらない() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            scope.Layout.UpdateLayout();
            float before = scope.GetFirstSection().Body.Size.y;

            AppendDecorationChild(scope);
            scope.Layout.UpdateLayout();

            Assert.That(scope.GetFirstSection().Body.Size.y, Is.EqualTo(before).Within(0.01f),
                "末端ぶんの長さが失われている．兄弟の並びに依存して判定している．");
        }

        [Test]
        public void ブロック直下に子を足してもヘッダーの幅が変わらない() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            scope.Layout.UpdateLayout();
            float before = scope.GetFirstSection().Header.Size.x;

            AppendDecorationChild(scope);
            scope.Layout.UpdateLayout();

            Assert.That(scope.GetFirstSection().Header.Size.x, Is.EqualTo(before).Within(0.01f));
        }

        [Test]
        public void 複数セクションでも末端ぶんは最後のセクションだけに乗る() {
            var multi = _env.CreateBlock(PrefabName.MultiScope);
            multi.Layout.UpdateLayout();

            var first = multi.Layout.Sections[0].Body;
            var last = multi.Layout.Sections[1].Body;

            Assert.That(last.Size.y, Is.GreaterThan(first.Size.y),
                "最後のセクションにだけ末端ぶんが加算されるはず．");
        }


        /// ----------------------------------------------------------------------------
        // ヘッダーの高さ

        /// <summary>
        /// 全アイテムの高さを揃えてレイアウトし、ヘッダーの高さを返す．
        /// </summary>
        private static float MeasureHeaderHeight(I_BPG_Block block, float itemHeight) {
            var header = block.GetFirstSection().Header;
            foreach (var item in header.Items) {
                item.RectTransform.sizeDelta = new Vector2(item.Size.x, itemHeight);
            }
            block.Layout.UpdateLayout();
            return header.Size.y;
        }

        [Test]
        public void 想定サイズ以下のアイテムではヘッダーが縮まない() {
            // [NOTE] 高さは「最小高さ＋想定サイズ(40)の超過分」で決まる．
            //        想定サイズ以下では、どれだけ小さくしても最小高さで頭打ちになる．
            var block = _env.CreateBlock(PrefabName.Normal);
            Assert.That(block.GetFirstSection().Header.Items, Is.Not.Empty,
                "前提: ヘッダーがアイテムを持つこと");

            float atBase = MeasureHeaderHeight(block, 40f);
            float belowBase = MeasureHeaderHeight(block, 10f);

            Assert.That(belowBase, Is.EqualTo(atBase).Within(0.01f),
                "小さいアイテムに合わせてヘッダーが縮んでいる．");
        }

        [Test]
        public void 大きいアイテムを入れると超過分だけヘッダーが伸びる() {
            var block = _env.CreateBlock(PrefabName.Normal);

            float atBase = MeasureHeaderHeight(block, 40f);
            float grown = MeasureHeaderHeight(block, 100f);

            Assert.That(grown, Is.EqualTo(atBase + 60f).Within(0.01f));
        }

        [Test]
        public void ヘッダーが伸びるとブロック全体も伸びる() {
            var block = _env.CreateBlock(PrefabName.Normal);

            MeasureHeaderHeight(block, 40f);
            float before = block.RectTransform.sizeDelta.y;

            MeasureHeaderHeight(block, 100f);

            Assert.That(block.RectTransform.sizeDelta.y, Is.EqualTo(before + 60f).Within(0.01f));
        }
    }
}
