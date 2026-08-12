using System.Linq;
using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// Canvas の表示倍率がブロックの生成結果に影響しないことを検証する．
    /// [NOTE] CanvasScaler を使う実環境では scaleFactor が 1 以外になる．
    ///        シーンルートに生成してから付け替えると、worldPositionStays による補正で
    ///        localScale に 1/scaleFactor が入り、ブロックが拡大表示されていた．
    /// </summary>
    public class BlockCreationScaleTest {

        private const float ScaleFactor = 0.5f;

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv(ScaleFactor);

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------

        [Test]
        public void 前提としてCanvasの倍率が1でない() {
            Assert.That(_env.Canvas.scaleFactor, Is.EqualTo(ScaleFactor));
        }

        [TestCase(PrefabName.Entry)]
        [TestCase(PrefabName.Normal)]
        [TestCase(PrefabName.Scope)]
        [TestCase(PrefabName.MultiScope)]
        public void 生成したブロックのスケールが等倍になる(string prefabName) {
            var block = _env.CreateBlock(prefabName);

            Assert.That(block.RectTransform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void 接続した子ブロックのスケールも等倍のままになる() {
            var parent = _env.CreateBlock(PrefabName.Scope);
            var child = _env.CreateBlock(PrefabName.Normal);

            parent.GetFirstSection().Body.AppendLast(child);

            Assert.That(child.RectTransform.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void 復元したブロックのスケールも等倍になる() {
            var data = new SerializableBlock("root", PrefabName.Scope, Vector3.zero);
            var section = new SerializableBlockSection();
            section.childBlocks.Add(new SerializableBlock("child", PrefabName.Normal, Vector3.zero));
            data.sections.Add(section);

            var block = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);

            var scales = block.GetAllChildBlocks(containSelf: true)
                .Select(b => b.RectTransform.localScale);
            Assert.That(scales, Is.All.EqualTo(Vector3.one));
        }
    }
}
