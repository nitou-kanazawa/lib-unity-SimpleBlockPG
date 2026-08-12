using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ブロックの配置結果が既知の値と一致することを検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] 期待値は uGUI の LayoutGroup で配置していた時代に実測した座標．
    ///        配置を自前化した際、参照したのが一部のプレハブだけだったために
    ///        Entry の余白が別のプレハブの値で上書きされる不具合を出した．
    ///        プレハブごとに設定が異なりうるため、全種類を対象に検証する．
    /// </remarks>
    public class BlockLayoutGeometryTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// 子ブロックを2つ持つ状態にして、レイアウトを確定させる．
        /// </summary>
        private I_BPG_Block BuildWithChildren(string prefabName, int sectionIndex = 0) {
            var block = _env.CreateBlock(prefabName);

            var body = block.Layout.Sections[sectionIndex].Body;
            if (body != null) {
                body.AppendLast(_env.CreateBlock(PrefabName.Normal));
                body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            }

            block.Layout.UpdateLayout();
            return block;
        }

        private static void AssertPosition(RectTransform rect, Vector2 expected, string label) {
            Assert.That(rect.anchoredPosition.x, Is.EqualTo(expected.x).Within(0.01f), $"{label} の x");
            Assert.That(rect.anchoredPosition.y, Is.EqualTo(expected.y).Within(0.01f), $"{label} の y");
        }


        /// ----------------------------------------------------------------------------
        // ヘッダー（アイテムの横並び）

        [Test]
        public void Entryのヘッダーアイテムが既知の位置に並ぶ() {
            // [NOTE] Entry だけ上余白が 30（他は 10）．ロゴ部分のぶん下がる．
            var block = BuildWithChildren(PrefabName.Entry);
            var header = block.Layout.Sections[0].Header.RectTransform;

            Assert.That(header.sizeDelta, Is.EqualTo(new Vector2(245.28f, 115f)));
            AssertPosition((RectTransform)header.GetChild(0), new Vector2(90.14f, -62.5f), "Item[0]");
            AssertPosition((RectTransform)header.GetChild(1), new Vector2(205.28f, -62.5f), "Item[1]");
        }

        [TestCase(PrefabName.Normal)]
        [TestCase(PrefabName.Scope)]
        [TestCase(PrefabName.MultiScope)]
        public void 通常ブロックのヘッダーアイテムが既知の位置に並ぶ(string prefabName) {
            var block = BuildWithChildren(prefabName);
            var header = block.Layout.Sections[0].Header.RectTransform;

            Assert.That(header.sizeDelta, Is.EqualTo(new Vector2(245.28f, 90f)));
            AssertPosition((RectTransform)header.GetChild(0), new Vector2(90.14f, -40f), "Item[0]");
            AssertPosition((RectTransform)header.GetChild(1), new Vector2(205.28f, -40f), "Item[1]");
        }


        /// ----------------------------------------------------------------------------
        // ボディ（子ブロックの縦積み）

        [Test]
        public void Entryの子ブロックは字下げされない() {
            // [NOTE] Entry だけ左余白が 0（他は 20）．
            var block = BuildWithChildren(PrefabName.Entry);
            var body = block.Layout.Sections[0].Body.RectTransform;

            Assert.That(body.sizeDelta, Is.EqualTo(new Vector2(245.28f, 150f)));
            AssertPosition((RectTransform)body.GetChild(0), new Vector2(0f, 10f), "Child[0]");
            AssertPosition((RectTransform)body.GetChild(1), new Vector2(0f, -70f), "Child[1]");
        }

        [TestCase(PrefabName.Scope, 0)]
        [TestCase(PrefabName.MultiScope, 0)]
        [TestCase(PrefabName.MultiScope, 1)]
        public void スコープブロックの子ブロックは左に字下げされる(string prefabName, int sectionIndex) {
            var block = BuildWithChildren(prefabName, sectionIndex);
            var body = block.Layout.Sections[sectionIndex].Body.RectTransform;

            // ※間隔が負なので、2つ目は 90 ではなく 80 ぶん下がる
            AssertPosition((RectTransform)body.GetChild(0), new Vector2(20f, 10f), "Child[0]");
            AssertPosition((RectTransform)body.GetChild(1), new Vector2(20f, -70f), "Child[1]");
        }


        /// ----------------------------------------------------------------------------
        // ブロック全体

        [Test]
        public void Entry全体の構成が既知の値と一致する() {
            var block = BuildWithChildren(PrefabName.Entry);
            var rect = block.RectTransform;
            var section = block.Layout.Sections[0].RectTransform;

            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(245.28f, 265f)));
            AssertPosition(section, new Vector2(122.64f, -132.5f), "Section");
            AssertPosition((RectTransform)rect.Find("OuterArea"), new Vector2(100f, -265f), "OuterArea");
        }

        [Test]
        public void Scope全体の構成が既知の値と一致する() {
            var block = BuildWithChildren(PrefabName.Scope);
            var rect = block.RectTransform;
            var section = block.Layout.Sections[0].RectTransform;

            // ※最後のセクションには下端のぶん 50 が加算される
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(245.28f, 290f)));
            AssertPosition(section, new Vector2(122.64f, -145f), "Section");
        }
    }
}
