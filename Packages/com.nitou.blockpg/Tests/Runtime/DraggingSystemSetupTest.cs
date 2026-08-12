using System;
using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.DragDrop;

namespace RuntimeTests {

    /// <summary>
    /// <see cref="DraggingSystem"/> の実行時セットアップを検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] シーンを介さずワークスペースを組み立てる用途を想定している．
    ///        従来はテスト側がリフレクションで private フィールドを書き換えており、
    ///        フィールド名の変更で静かに壊れる状態だった．
    /// </remarks>
    public class DraggingSystemSetupTest {

        private GameObject _systemObj;
        private GameObject _holderObj;

        [SetUp]
        public void SetUp() {
            _holderObj = new GameObject("[Test] Holder", typeof(RectTransform));
            _systemObj = new GameObject("[Test] DraggingSystem");
        }

        [TearDown]
        public void TearDown() {
            if (_systemObj != null) UnityEngine.Object.DestroyImmediate(_systemObj);
            if (_holderObj != null) UnityEngine.Object.DestroyImmediate(_holderObj);
        }


        /// ----------------------------------------------------------------------------

        [Test]
        public void 配置先を設定できる() {
            var system = _systemObj.AddComponent<DraggingSystem>();

            system.Setup(_holderObj.transform);

            Assert.That(system.DraggingHolder, Is.EqualTo(_holderObj.transform));
        }

        [Test]
        public void 予告表示は省略できる() {
            // [NOTE] GhostBlock が無くても接続そのものは成立する．
            var system = _systemObj.AddComponent<DraggingSystem>();

            system.Setup(_holderObj.transform);

            Assert.That(system.GhostBlock, Is.Null);
        }

        [Test]
        public void 予告表示を設定できる() {
            var system = _systemObj.AddComponent<DraggingSystem>();
            var ghostObj = new GameObject("[Test] Ghost", typeof(RectTransform));
            try {
                var ghost = ghostObj.AddComponent<BPG_GhostBlock>();

                system.Setup(_holderObj.transform, ghost);

                Assert.That(system.GhostBlock, Is.EqualTo(ghost));
            } finally {
                UnityEngine.Object.DestroyImmediate(ghostObj);
            }
        }

        [Test]
        public void 検出距離を指定しなければ既定値が保たれる() {
            var system = _systemObj.AddComponent<DraggingSystem>();
            float original = system.DetectionDistance;

            system.Setup(_holderObj.transform);

            Assert.That(system.DetectionDistance, Is.EqualTo(original));
        }

        [Test]
        public void 検出距離を指定できる() {
            var system = _systemObj.AddComponent<DraggingSystem>();

            system.Setup(_holderObj.transform, detectionDistance: 120f);

            Assert.That(system.DetectionDistance, Is.EqualTo(120f));
        }

        [Test]
        public void 負の検出距離はゼロに丸められる() {
            var system = _systemObj.AddComponent<DraggingSystem>();

            system.Setup(_holderObj.transform, detectionDistance: -10f);

            Assert.That(system.DetectionDistance, Is.Zero);
        }

        [Test]
        public void 配置先がnullなら例外になる() {
            // [NOTE] 未設定だとドラッグしたブロックが親なしへ飛ばされて画面から消える．
            //        黙って動かないより、その場で気づけるようにする．
            var system = _systemObj.AddComponent<DraggingSystem>();

            Assert.Throws<ArgumentNullException>(() => system.Setup(null));
        }
    }
}
