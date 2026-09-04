using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using nitou.BlockPG.DragDrop;
using nitou.BlockPG.Events;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ブロックのライフサイクルイベントを検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] イベントバスは静的なため、購読はテストごとに必ず破棄する．
    /// </remarks>
    public class BlockEventTest {

        private BlockPGTestEnv _env;
        private CompositeDisposable _subscriptions;

        [SetUp]
        public void SetUp() {
            _env = new BlockPGTestEnv(canvasScaleFactor: 1f, withDraggingSystem: true);
            _subscriptions = new CompositeDisposable();
        }

        [TearDown]
        public void TearDown() {
            _subscriptions.Dispose();
            _env.Dispose();
        }


        /// ----------------------------------------------------------------------------
        // Helper

        private void Subscribe<T>(IObservable<T> observable, List<T> sink) {
            observable.Subscribe(sink.Add).AddTo(_subscriptions);
        }

        private PointerEventData PointerAt(Vector2 worldPoint) {
            return new PointerEventData(EventSystem.current) {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(_env.Canvas.worldCamera, worldPoint),
            };
        }

        private void DragTo(I_BPG_Block block, Vector2 destination) {
            var drag = block.RectTransform.GetComponent<BPG_BlockDraggingBase>();
            ((IBeginDragHandler)drag).OnBeginDrag(PointerAt(block.RectTransform.position));

            var move = PointerAt(destination);
            ((IDragHandler)drag).OnDrag(move);
            ((IEndDragHandler)drag).OnEndDrag(move);
        }


        /// ----------------------------------------------------------------------------
        // 生成と破棄

        [Test]
        public void 生成イベントが発火する() {
            var created = new List<I_BPG_Block>();
            Subscribe(BPG_BlockEventBus.OnCreated, created);

            var block = _env.CreateBlock(PrefabName.Scope);

            Assert.That(created, Has.Count.EqualTo(1));
            Assert.That(created[0], Is.EqualTo(block));
        }

        [Test]
        public void 破棄イベントが子孫ぶん発火する() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            scope.GetFirstSection().Body.AppendLast(_env.CreateBlock(PrefabName.Normal));

            var destroyed = new List<I_BPG_Block>();
            Subscribe(BPG_BlockEventBus.OnDestroyed, destroyed);

            nitou.BlockPG.Blocks.BPG_BlockUtils.RemoveBlock(scope);

            Assert.That(destroyed, Has.Count.EqualTo(2));
        }

        [Test]
        public void 復元による作り直しでは破棄イベントが飛ばない() {
            // [NOTE] Undo/Redo の作り直しを、利用者による削除と区別するため．
            _env.CreateBlock(PrefabName.Scope);

            var destroyed = new List<I_BPG_Block>();
            Subscribe(BPG_BlockEventBus.OnDestroyed, destroyed);

            _env.ProgrammingEnv.RemoveAllBlocks();

            Assert.That(destroyed, Is.Empty);
        }


        /// ----------------------------------------------------------------------------
        // ドラッグ

        [UnityTest]
        public IEnumerator ドラッグ開始と終了のイベントが発火する() {
            var block = _env.CreateBlock(PrefabName.Normal);
            yield return null;

            var started = new List<BlockLocationEvent>();
            var ended = new List<BlockLocationEvent>();
            Subscribe(BPG_BlockEventBus.OnStartDrag, started);
            Subscribe(BPG_BlockEventBus.OnEndDrag, ended);

            DragTo(block, new Vector2(9999f, 9999f));

            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(ended, Has.Count.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator 接続先が無い場所へのドロップはOutsideとして通知される() {
            var block = _env.CreateBlock(PrefabName.Normal);
            yield return null;

            var moves = new List<BlockMoveEvent>();
            Subscribe(BPG_BlockEventBus.OnMove, moves);

            DragTo(block, new Vector2(9999f, 9999f));

            Assert.That(moves, Has.Count.EqualTo(1));
            Assert.That(moves[0].From, Is.EqualTo(BlockLocation.ProgEnv));
            Assert.That(moves[0].To, Is.EqualTo(BlockLocation.Outside));
        }

        [UnityTest]
        public IEnumerator セクションへの接続はStackとして通知される() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var normal = _env.CreateBlock(PrefabName.Normal);
            scope.RectTransform.anchoredPosition = new Vector2(200f, -150f);
            normal.RectTransform.anchoredPosition = new Vector2(700f, -400f);
            Canvas.ForceUpdateCanvases();
            yield return null;

            var moves = new List<BlockMoveEvent>();
            Subscribe(BPG_BlockEventBus.OnMove, moves);

            DragTo(normal, scope.GetFirstSection().Body.Spot.DropPosition);

            Assert.That(moves, Has.Count.EqualTo(1));
            Assert.That(moves[0].From, Is.EqualTo(BlockLocation.ProgEnv));
            Assert.That(moves[0].To, Is.EqualTo(BlockLocation.Stack));
        }

        [UnityTest]
        public IEnumerator 接続済みブロックのドラッグ開始はStackとして通知される() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var normal = _env.CreateBlock(PrefabName.Normal);
            scope.GetFirstSection().Body.AppendLast(normal);
            yield return null;

            var started = new List<BlockLocationEvent>();
            Subscribe(BPG_BlockEventBus.OnStartDrag, started);

            var drag = normal.RectTransform.GetComponent<BPG_BlockDraggingBase>();
            ((IBeginDragHandler)drag).OnBeginDrag(PointerAt(normal.RectTransform.position));

            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(started[0].Location, Is.EqualTo(BlockLocation.Stack));
        }


        /// ----------------------------------------------------------------------------
        // 操作の分類

        [TestCase(BlockLocation.Outside, BlockLocation.ProgEnv, DraggingResult.CreateBlock)]
        [TestCase(BlockLocation.ProgEnv, BlockLocation.Outside, DraggingResult.DestroyBlock)]
        [TestCase(BlockLocation.ProgEnv, BlockLocation.ProgEnv, DraggingResult.FreeMove)]
        [TestCase(BlockLocation.ProgEnv, BlockLocation.Stack, DraggingResult.Move)]
        public void 移動元と移動先から操作の種類を判定できる(
            BlockLocation from, BlockLocation to, DraggingResult expected) {

            Assert.That(DraggingUtil.CheckResult(from, to), Is.EqualTo(expected));
        }
    }
}
