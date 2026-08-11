using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// 積み上げ対象から子を除外できることを検証する．
    /// [NOTE] ブロック直下とセクションの縦積みは自前で行っている．
    ///        LayoutGroup が持っていた ignoreLayout 相当の逃げ道が無いと、
    ///        選択枠やバッジなどの装飾を重ねられなくなる．
    /// </summary>
    public class BlockLayoutIgnoreTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// 装飾用のオーバーレイをブロック直下へ追加する．
        /// </summary>
        private static RectTransform AddOverlay(I_BPG_Block block, bool ignoreLayout, bool ignorerEnabled = true) {
            var obj = new GameObject("Overlay", typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(block.RectTransform, worldPositionStays: false);
            rect.sizeDelta = new Vector2(40f, 40f);

            var element = obj.AddComponent<LayoutElement>();
            element.ignoreLayout = ignoreLayout;
            element.enabled = ignorerEnabled;

            rect.anchoredPosition = new Vector2(-100f, 100f);
            return rect;
        }


        /// ----------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ignoreLayoutを有効にした子は位置を動かされない() {
            var block = _env.CreateBlock(PrefabName.Scope);
            var overlay = AddOverlay(block, ignoreLayout: true);
            var placed = overlay.anchoredPosition;

            block.Layout.SetLayoutDirty();
            yield return null;
            yield return null;

            Assert.That(overlay.anchoredPosition, Is.EqualTo(placed),
                "ignoreLayout を有効にした子が積み上げに巻き込まれている．");
        }

        [UnityTest]
        public IEnumerator ignoreLayoutを有効にした子は積み上げ位置に影響しない() {
            // Arrange : オーバーレイ無しでの各セクションの位置を控えておく
            var reference = _env.CreateBlock(PrefabName.MultiScope);
            reference.Layout.UpdateLayout();
            var expected = new Vector2[reference.Layout.Sections.Count];
            for (int i = 0; i < expected.Length; i++) {
                expected[i] = reference.Layout.Sections[i].RectTransform.anchoredPosition;
            }

            // Act : 先頭にオーバーレイを差し込む
            var block = _env.CreateBlock(PrefabName.MultiScope);
            var overlay = AddOverlay(block, ignoreLayout: true);
            overlay.SetSiblingIndex(0);

            block.Layout.SetLayoutDirty();
            yield return null;
            yield return null;

            // Assert : セクションの位置が変わっていない
            for (int i = 0; i < expected.Length; i++) {
                Assert.That(block.Layout.Sections[i].RectTransform.anchoredPosition,
                    Is.EqualTo(expected[i]), $"セクション {i} の位置がずれている．");
            }
        }

        [UnityTest]
        public IEnumerator ignoreLayoutが無効な子は積み上げ対象になる() {
            var block = _env.CreateBlock(PrefabName.Scope);
            var overlay = AddOverlay(block, ignoreLayout: false);
            var placed = overlay.anchoredPosition;

            block.Layout.SetLayoutDirty();
            yield return null;
            yield return null;

            Assert.That(overlay.anchoredPosition, Is.Not.EqualTo(placed),
                "ignoreLayout が無効な子は従来どおり積み上げられるべき．");
        }

        [UnityTest]
        public IEnumerator コンポーネントが無効なら指定は無視される() {
            // [NOTE] LayoutGroup と同じ挙動に揃える．
            var block = _env.CreateBlock(PrefabName.Scope);
            var overlay = AddOverlay(block, ignoreLayout: true, ignorerEnabled: false);
            var placed = overlay.anchoredPosition;

            block.Layout.SetLayoutDirty();
            yield return null;
            yield return null;

            Assert.That(overlay.anchoredPosition, Is.Not.EqualTo(placed),
                "無効化された LayoutElement の指定が尊重されている．");
        }

        [UnityTest]
        public IEnumerator 非アクティブな子は積み上げ対象から外れる() {
            var block = _env.CreateBlock(PrefabName.Scope);
            var overlay = AddOverlay(block, ignoreLayout: false);
            overlay.gameObject.SetActive(false);
            var placed = overlay.anchoredPosition;

            block.Layout.SetLayoutDirty();
            yield return null;
            yield return null;

            Assert.That(overlay.anchoredPosition, Is.EqualTo(placed));
        }
    }
}
