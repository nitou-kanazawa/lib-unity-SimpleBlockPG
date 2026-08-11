using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using nitou.BlockPG.Interface;
using Debug = UnityEngine.Debug;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// レイアウト更新の計算量とコスト構造に関するテスト．
    /// [NOTE] 絶対時間はマシン性能に左右されるため、しきい値には使わない．
    ///        「規模に対する増え方」と「構造上の上限」で縛り、実測値はログに残す．
    /// </summary>
    [Category("Performance")]
    public class BlockLayoutPerformanceTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// Scope を深さ方向に連ね、各階層に Normal を2つぶら下げた木を作る．
        /// </summary>
        private I_BPG_Block BuildChain(int depth) {
            var root = _env.CreateBlock(PrefabName.Scope);
            var current = root;

            for (int i = 0; i < depth; i++) {
                var body = current.GetFirstSection().Body;
                body.AppendLast(_env.CreateBlock(PrefabName.Normal));
                body.AppendLast(_env.CreateBlock(PrefabName.Normal));

                var next = _env.CreateBlock(PrefabName.Scope);
                body.AppendLast(next);
                current = next;
            }
            return root;
        }

        /// <summary>
        /// 複数回試行して最小値を取る．（※外乱の影響を避けるため平均は使わない）
        /// </summary>
        private static double MeasureMinMs(System.Action action, int trials = 5, int iterations = 30) {
            // ウォームアップ
            for (int i = 0; i < iterations; i++) action();

            double best = double.MaxValue;
            var stopwatch = new Stopwatch();
            for (int t = 0; t < trials; t++) {
                stopwatch.Restart();
                for (int i = 0; i < iterations; i++) action();
                stopwatch.Stop();

                double perCall = stopwatch.Elapsed.TotalMilliseconds / iterations;
                if (perCall < best) best = perCall;
            }
            return best;
        }


        /// ----------------------------------------------------------------------------
        // 計算量

        [Test]
        public void レイアウト更新はブロック数に対して線形に増える() {
            // [NOTE] ブロック数を約2倍にしたとき、線形なら約2倍、
            //        計算量が悪化していれば4倍前後になる．3.0倍を境界とする．
            var small = BuildChain(4);
            int smallCount = small.GetAllChaildBlocksCount(containSelf: true);
            double smallMs = MeasureMinMs(() => small.Layout.UpdateLayout());

            var large = BuildChain(9);
            int largeCount = large.GetAllChaildBlocksCount(containSelf: true);
            double largeMs = MeasureMinMs(() => large.Layout.UpdateLayout());

            double sizeRatio = largeCount / (double)smallCount;
            double timeRatio = largeMs / smallMs;

            Debug.Log($"[Layout] blocks {smallCount} -> {largeCount} ({sizeRatio:F2}x) / " +
                $"time {smallMs:F4}ms -> {largeMs:F4}ms ({timeRatio:F2}x)");

            Assert.That(sizeRatio, Is.GreaterThan(1.8), "前提: 規模がおよそ2倍になっていること");
            Assert.That(timeRatio, Is.LessThan(3.0),
                $"レイアウト更新の計算量が悪化している．規模 {sizeRatio:F2} 倍に対し時間 {timeRatio:F2} 倍．");
        }

        [Test]
        public void ブロックのルートはuGUIのレイアウトルートではない() {
            // [NOTE] ブロック自身とセクションの縦積みは自前で行っている．
            //        ルートに ILayoutController が付くと uGUI の再構築が
            //        ブロックの部分木全体へ降りてくるため、それを防ぐ．
            //
            //        LayoutRebuilder は ILayoutController を持たない階層で走査を打ち切るので、
            //        ここが空であることが「uGUI がブロックを辿らない」ことと等しい．
            var root = BuildChain(6);
            var rect = root.RectTransform;

            Assert.That(rect.GetComponent<ILayoutController>(), Is.Null,
                "ブロックのルートに LayoutGroup 等が付いている．");

            // 実測値は推移を追えるようログに残す
            double customMs = MeasureMinMs(() => root.Layout.UpdateLayout());
            double ugui = MeasureMinMs(() => LayoutRebuilder.ForceRebuildLayoutImmediate(rect));

            int blocks = root.GetAllChaildBlocksCount(containSelf: true);
            int groups = rect.GetComponentsInChildren<LayoutGroup>(true).Length;

            Debug.Log($"[Layout] blocks={blocks} layoutGroups={groups} / " +
                $"UpdateLayout={customMs:F4}ms  ForceRebuildFromRoot={ugui:F4}ms");

            Assert.That(ugui, Is.LessThan(customMs),
                "ブロックのルートから uGUI の再構築が走っている．");
        }


        /// ----------------------------------------------------------------------------
        // 構造上の上限

        [TestCase(PrefabName.Entry, 2)]
        [TestCase(PrefabName.Normal, 1)]
        [TestCase(PrefabName.Scope, 2)]
        [TestCase(PrefabName.MultiScope, 4)]
        public void ブロックあたりのLayoutGroup数が上限内に収まる(string prefabName, int maxGroups) {
            // [NOTE] LayoutGroup はブロックの入れ子ぶんだけ再帰的に増える．
            //        不用意に増えると再構築コストが直接効いてくるため上限で縛る．
            //        削減する改修を入れたら、この上限も併せて下げること．
            var block = _env.CreateBlock(prefabName);

            int groups = block.RectTransform.GetComponentsInChildren<LayoutGroup>(true).Length;
            Debug.Log($"[Layout] {prefabName} layoutGroups={groups} (max {maxGroups})");

            Assert.That(groups, Is.LessThanOrEqualTo(maxGroups));
        }

        [Test]
        public void LayoutGroupは子のサイズを制御しない設定になっている() {
            // [NOTE] childControl を有効にすると、親子でサイズを問い合わせ合う多段パスが走り、
            //        入れ子構造では計算量が跳ね上がる．サイズはライブラリ側が決めるため不要．
            var block = _env.CreateBlock(PrefabName.MultiScope);

            foreach (var group in block.RectTransform.GetComponentsInChildren<HorizontalOrVerticalLayoutGroup>(true)) {
                Assert.That(group.childControlWidth, Is.False, $"{group.name} が子の幅を制御している．");
                Assert.That(group.childControlHeight, Is.False, $"{group.name} が子の高さを制御している．");
            }
        }


        /// ----------------------------------------------------------------------------
        // 無駄な更新が走らないこと

        [UnityTest]
        public IEnumerator 構成が変化しないフレームではレイアウト更新が走らない() {
            // [NOTE] dirtyフラグが機能していることを、観測可能な形で確認する．
            //        更新が走っていれば、崩したサイズが元に戻る．
            var root = BuildChain(3);
            yield return null;
            yield return null;

            var target = root.GetFirstSection().Body.ChildBlocks[0];
            var broken = new Vector2(1f, 1f);
            target.RectTransform.sizeDelta = broken;

            yield return null;
            yield return null;

            Assert.That(target.RectTransform.sizeDelta, Is.EqualTo(broken),
                "構成が変化していないのにレイアウト更新が走っている．");
        }
    }
}
