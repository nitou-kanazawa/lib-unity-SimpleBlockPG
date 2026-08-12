using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// 横方向のブロックレイアウトを検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] 現時点で横レイアウトを使うプレハブは無いため、
    ///        既存プレハブのレイアウトコンポーネントを差し替えて構成する．
    /// </remarks>
    public class BlockHorizontalLayoutTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// レイアウトを横方向へ差し替えたブロックを作る．
        /// </summary>
        /// <remarks>
        /// [NOTE] 非アクティブな親の下で生成することで Awake を保留し、
        ///        差し替えを終えてから有効化する．
        ///        アクティブなまま差し替えると、各コンポーネントが Awake で取得した
        ///        レイアウトの参照が破棄済みのまま残る．
        /// </remarks>
        private I_BPG_Block CreateHorizontalBlock(string prefabName) {
            var prefab = BPG_BlockUtils.LoadBlockPrefab(prefabName);
            Assert.That(prefab, Is.Not.Null, $"前提: プレハブが存在すること ({prefabName})");

            var holder = new GameObject("[Test] Holder", typeof(RectTransform));
            holder.SetActive(false);
            holder.transform.SetParent(_env.ProgrammingEnv.RectTransform, worldPositionStays: false);

            var obj = Object.Instantiate(prefab.gameObject, holder.transform);
            obj.name = prefabName;

            Object.DestroyImmediate(obj.GetComponent<BPG_BlockVerticalLayout>());
            obj.AddComponent<BPG_BlockHorizontalLayout>();

            // ※ここで初めて Awake が走る
            obj.transform.SetParent(_env.ProgrammingEnv.RectTransform, worldPositionStays: false);
            Object.DestroyImmediate(holder);

            var block = obj.GetComponent<I_BPG_Block>();
            block.SetParentSection(null);
            return block;
        }


        /// ----------------------------------------------------------------------------
        // 方向の伝播

        [Test]
        public void 横レイアウトの軸がセクションまで伝わる() {
            var block = CreateHorizontalBlock(PrefabName.Scope);

            Assert.That(block.Layout.Axis, Is.EqualTo(BlockLayoutAxis.Horizontal));
            Assert.That(block.Layout.Sections[0].Axis, Is.EqualTo(BlockLayoutAxis.Horizontal));
        }

        [Test]
        public void 既定は縦のままになる() {
            var block = _env.CreateBlock(PrefabName.Scope);

            Assert.That(block.Layout.Axis, Is.EqualTo(BlockLayoutAxis.Vertical));
            Assert.That(block.Layout.Sections[0].Axis, Is.EqualTo(BlockLayoutAxis.Vertical));
        }


        /// ----------------------------------------------------------------------------
        // サイズの合成

        [Test]
        public void 横レイアウトではセクションが横に並ぶ() {
            // [NOTE] MultiScope は2セクション．縦なら高さ、横なら幅が合計になる．
            var vertical = _env.CreateBlock(PrefabName.MultiScope);
            vertical.Layout.UpdateLayout();
            var verticalSize = vertical.RectTransform.sizeDelta;

            var horizontal = CreateHorizontalBlock(PrefabName.MultiScope);
            horizontal.Layout.UpdateLayout();
            var horizontalSize = horizontal.RectTransform.sizeDelta;

            var sections = horizontal.Layout.Sections;
            float widthSum = 0f;
            float heightMax = 0f;
            foreach (var section in sections) {
                widthSum += section.Size.x;
                heightMax = Mathf.Max(heightMax, section.Size.y);
            }

            Assert.That(horizontalSize.x, Is.EqualTo(widthSum).Within(0.01f), "幅がセクションの合計になっていない");
            Assert.That(horizontalSize.y, Is.EqualTo(heightMax).Within(0.01f), "高さがセクションの最大値になっていない");
            Assert.That(horizontalSize.x, Is.GreaterThan(verticalSize.x), "縦のときより横に広がるはず");
        }

        [Test]
        public void 横レイアウトではセクションが左から順に配置される() {
            var block = CreateHorizontalBlock(PrefabName.MultiScope);
            block.Layout.UpdateLayout();

            var first = block.Layout.Sections[0].RectTransform;
            var second = block.Layout.Sections[1].RectTransform;

            // ※pivot を考慮した左端で比較する
            float firstLeft = first.anchoredPosition.x - first.sizeDelta.x * first.pivot.x;
            float secondLeft = second.anchoredPosition.x - second.sizeDelta.x * second.pivot.x;

            Assert.That(firstLeft, Is.EqualTo(0f).Within(0.01f));
            Assert.That(secondLeft, Is.EqualTo(first.sizeDelta.x).Within(0.01f));
        }

        [Test]
        public void 横レイアウトでは子ブロックが横に並ぶ() {
            var block = CreateHorizontalBlock(PrefabName.Scope);
            var body = block.Layout.Sections[0].Body;
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            block.Layout.UpdateLayout();

            var bodyRect = body.RectTransform;
            var child0 = (RectTransform)bodyRect.GetChild(0);
            var child1 = (RectTransform)bodyRect.GetChild(1);

            // 横に並ぶので x がずれ、y は揃う
            Assert.That(child1.anchoredPosition.x, Is.GreaterThan(child0.anchoredPosition.x));
            Assert.That(child1.anchoredPosition.y, Is.EqualTo(child0.anchoredPosition.y).Within(0.01f));
        }

        [Test]
        public void 横レイアウトでもサイズの整合が保たれる() {
            // [NOTE] 縦の場合と同じ不変条件（実サイズ == 計算値）を確認する．
            var block = CreateHorizontalBlock(PrefabName.Scope);
            var body = block.Layout.Sections[0].Body;
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            block.Layout.UpdateLayout();

            Assert.That(block.RectTransform.sizeDelta, Is.EqualTo(block.Layout.Size));
        }
    }
}
