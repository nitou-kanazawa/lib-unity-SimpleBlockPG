using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using nitou.BlockPG.Events;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ブロックへのポインタ操作が、意味論イベントへ変換されることを検証する．
    /// </summary>
    public class BlockInteractionTest {

        // ※テストを長引かせないため、しきい値は短く設定する
        private const float HoldSeconds = 0.1f;

        // マウスは負、タッチは 0 以上の pointerId が割り当てられる
        private const int MousePointerId = -1;
        private const int TouchPointerId = 0;

        private BlockPGTestEnv _env;
        private CompositeDisposable _subscriptions;

        [SetUp]
        public void SetUp() {
            _env = new BlockPGTestEnv();
            _subscriptions = new CompositeDisposable();
        }

        [TearDown]
        public void TearDown() {
            _subscriptions.Dispose();
            _env.Dispose();
        }


        /// ----------------------------------------------------------------------------
        // Helper

        private (I_BPG_Block block, BPG_BlockInteraction interaction) CreateBlock() {
            var block = _env.CreateBlock(PrefabName.Normal);
            var interaction = block.RectTransform.GetComponent<BPG_BlockInteraction>();
            Assert.That(interaction, Is.Not.Null, "前提: BPG_BlockInteraction が付いていること");

            interaction.MouseHoldSeconds = HoldSeconds;
            interaction.TouchHoldSeconds = HoldSeconds;
            return (block, interaction);
        }

        private void Subscribe<T>(IObservable<T> observable, List<T> sink) {
            observable.Subscribe(sink.Add).AddTo(_subscriptions);
        }

        private static PointerEventData Pointer(int pointerId,
            PointerEventData.InputButton button = PointerEventData.InputButton.Left, int clickCount = 1) {

            return new PointerEventData(EventSystem.current) {
                pointerId = pointerId,
                button = button,
                clickCount = clickCount,
                position = new Vector2(100f, 100f),
            };
        }

        private static void Press(BPG_BlockInteraction target, PointerEventData e) =>
            ((IPointerDownHandler)target).OnPointerDown(e);

        private static void Release(BPG_BlockInteraction target, PointerEventData e) =>
            ((IPointerUpHandler)target).OnPointerUp(e);

        private static void Click(BPG_BlockInteraction target, PointerEventData e) =>
            ((IPointerClickHandler)target).OnPointerClick(e);

        private static void BeginDrag(BPG_BlockInteraction target, PointerEventData e) =>
            ((IBeginDragHandler)target).OnBeginDrag(e);


        /// ----------------------------------------------------------------------------
        // 主操作

        [Test]
        public void クリックで主操作が発火する() {
            var (block, interaction) = CreateBlock();
            var actions = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnPrimaryAction, actions);

            Click(interaction, Pointer(MousePointerId));

            Assert.That(actions, Has.Count.EqualTo(1));
            Assert.That(actions[0].Block, Is.EqualTo(block));
            Assert.That(actions[0].Source, Is.EqualTo(PointerSource.Mouse));
        }

        [Test]
        public void タップでも同じ主操作が発火する() {
            var (_, interaction) = CreateBlock();
            var actions = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnPrimaryAction, actions);

            Click(interaction, Pointer(TouchPointerId));

            Assert.That(actions, Has.Count.EqualTo(1));
            Assert.That(actions[0].Source, Is.EqualTo(PointerSource.Touch));
        }

        [Test]
        public void 二度押しは二度押しとして発火する() {
            var (_, interaction) = CreateBlock();
            var primary = new List<BlockPointerEvent>();
            var doubles = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnPrimaryAction, primary);
            Subscribe(BPG_BlockEventBus.OnDoubleAction, doubles);

            Click(interaction, Pointer(MousePointerId, clickCount: 2));

            Assert.That(doubles, Has.Count.EqualTo(1));
            Assert.That(primary, Is.Empty);
        }


        /// ----------------------------------------------------------------------------
        // 副次操作

        [Test]
        public void 右クリックで副次操作が発火する() {
            var (_, interaction) = CreateBlock();
            var actions = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnSecondaryAction, actions);

            Click(interaction, Pointer(MousePointerId, PointerEventData.InputButton.Right));

            Assert.That(actions, Has.Count.EqualTo(1));
        }

        [Test]
        public void 右クリックでは主操作が発火しない() {
            var (_, interaction) = CreateBlock();
            var primary = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnPrimaryAction, primary);

            Click(interaction, Pointer(MousePointerId, PointerEventData.InputButton.Right));

            Assert.That(primary, Is.Empty);
        }

        [UnityTest]
        public IEnumerator 長押しで副次操作が発火する() {
            // [NOTE] 右クリックと長押しが同じイベントへ束ねられることの確認．
            var (_, interaction) = CreateBlock();
            var actions = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnSecondaryAction, actions);

            Press(interaction, Pointer(TouchPointerId));
            yield return new WaitForSecondsRealtime(HoldSeconds * 3f);

            Assert.That(actions, Has.Count.EqualTo(1));
            Assert.That(actions[0].Source, Is.EqualTo(PointerSource.Touch));
        }

        [UnityTest]
        public IEnumerator 長押しは指を離す前に発火する() {
            var (_, interaction) = CreateBlock();
            var actions = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnSecondaryAction, actions);

            Press(interaction, Pointer(TouchPointerId));
            yield return new WaitForSecondsRealtime(HoldSeconds * 3f);

            // ※まだ離していない時点で発火している
            Assert.That(actions, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator しきい値前に離せば長押しにならない() {
            var (_, interaction) = CreateBlock();
            var actions = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnSecondaryAction, actions);

            var pointer = Pointer(TouchPointerId);
            Press(interaction, pointer);
            Release(interaction, pointer);

            yield return new WaitForSecondsRealtime(HoldSeconds * 3f);

            Assert.That(actions, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ドラッグへ移行すれば長押しにならない() {
            var (_, interaction) = CreateBlock();
            var actions = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnSecondaryAction, actions);

            var pointer = Pointer(TouchPointerId);
            Press(interaction, pointer);
            BeginDrag(interaction, pointer);

            yield return new WaitForSecondsRealtime(HoldSeconds * 3f);

            Assert.That(actions, Is.Empty);
        }

        [UnityTest]
        public IEnumerator 長押しの後に離しても主操作は発火しない() {
            // [NOTE] メニューを出したうえでクリック扱いにもなると二重に反応してしまう．
            var (_, interaction) = CreateBlock();
            var primary = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnPrimaryAction, primary);

            var pointer = Pointer(TouchPointerId);
            Press(interaction, pointer);
            yield return new WaitForSecondsRealtime(HoldSeconds * 3f);

            Release(interaction, pointer);
            Click(interaction, pointer);

            Assert.That(primary, Is.Empty);
        }

        [UnityTest]
        public IEnumerator 長押しの次のクリックは通常どおり発火する() {
            var (_, interaction) = CreateBlock();
            var primary = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnPrimaryAction, primary);

            var pointer = Pointer(TouchPointerId);
            Press(interaction, pointer);
            yield return new WaitForSecondsRealtime(HoldSeconds * 3f);
            Release(interaction, pointer);
            Click(interaction, pointer);

            // 2回目は普通のタップ
            Press(interaction, pointer);
            Release(interaction, pointer);
            Click(interaction, pointer);

            Assert.That(primary, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ポーズ中でも長押しが成立する() {
            // [NOTE] UniRx の既定スケジューラは Time.timeScale の影響を受ける．
            //        MainThreadIgnoreTimeScale を使わないとポーズ中に永久に成立しない．
            var (_, interaction) = CreateBlock();
            var actions = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnSecondaryAction, actions);

            float original = Time.timeScale;
            try {
                Time.timeScale = 0f;

                Press(interaction, Pointer(TouchPointerId));
                yield return new WaitForSecondsRealtime(HoldSeconds * 3f);

                Assert.That(actions, Has.Count.EqualTo(1),
                    "ポーズ中に長押しが成立していない．スケジューラを確認．");
            } finally {
                Time.timeScale = original;
            }
        }


        /// ----------------------------------------------------------------------------
        // ホバー

        [Test]
        public void マウスではホバーが発火する() {
            var (_, interaction) = CreateBlock();
            var enters = new List<BlockPointerEvent>();
            var exits = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnHoverEnter, enters);
            Subscribe(BPG_BlockEventBus.OnHoverExit, exits);

            ((IPointerEnterHandler)interaction).OnPointerEnter(Pointer(MousePointerId));
            ((IPointerExitHandler)interaction).OnPointerExit(Pointer(MousePointerId));

            Assert.That(enters, Has.Count.EqualTo(1));
            Assert.That(exits, Has.Count.EqualTo(1));
        }

        [Test]
        public void タッチではホバーが発火しない() {
            // [NOTE] タッチは押下時に「乗った」ことになるため、ホバーとしては扱わない．
            var (_, interaction) = CreateBlock();
            var enters = new List<BlockPointerEvent>();
            Subscribe(BPG_BlockEventBus.OnHoverEnter, enters);

            ((IPointerEnterHandler)interaction).OnPointerEnter(Pointer(TouchPointerId));

            Assert.That(enters, Is.Empty);
        }


        /// ----------------------------------------------------------------------------
        // 発生源の判定

        [TestCase(-1, PointerSource.Mouse)]
        [TestCase(-2, PointerSource.Mouse)]
        [TestCase(0, PointerSource.Touch)]
        [TestCase(3, PointerSource.Touch)]
        public void ポインタIDから発生源を判定できる(int pointerId, PointerSource expected) {
            Assert.That(BlockPointerEvent.GetSource(Pointer(pointerId)), Is.EqualTo(expected));
        }
    }
}
