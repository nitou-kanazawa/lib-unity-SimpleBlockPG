using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using nitou.BlockPG.DragDrop;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ドラッグ&amp;ドロップによるブロック接続のテスト．
    /// [NOTE] UGUI のイベントハンドラを直接呼ぶことで、実際の操作経路を再現する．
    /// </summary>
    public class BlockDragDropTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv(canvasScaleFactor: 1f, withDraggingSystem: true);

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// ----------------------------------------------------------------------------
        // Helper

        private PointerEventData PointerAt(Vector2 worldPoint) {
            var camera = _env.Canvas.worldCamera;
            return new PointerEventData(EventSystem.current) {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(camera, worldPoint),
            };
        }

        /// <summary>
        /// 対象ブロックを掴んで、指定のワールド座標へ運んで離す．
        /// </summary>
        private void DragTo(I_BPG_Block block, Vector2 destination) {
            var drag = block.RectTransform.GetComponent<BPG_BlockDraggingBase>();

            var begin = PointerAt(block.RectTransform.position);
            ((IBeginDragHandler)drag).OnBeginDrag(begin);

            var move = PointerAt(destination);
            ((IDragHandler)drag).OnDrag(move);
            ((IEndDragHandler)drag).OnEndDrag(move);
        }


        /// ----------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ドラッグ開始でドラッグ用の階層へ移動する() {
            var block = _env.CreateBlock(PrefabName.Normal);
            yield return null;

            var drag = block.RectTransform.GetComponent<BPG_BlockDraggingBase>();
            ((IBeginDragHandler)drag).OnBeginDrag(PointerAt(block.RectTransform.position));

            Assert.That(drag.IsDragging, Is.True);
            Assert.That(block.RectTransform.parent, Is.EqualTo(_env.DraggingLayer));
        }

        [UnityTest]
        public IEnumerator セクションへドロップすると子ブロックとして接続される() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var normal = _env.CreateBlock(PrefabName.Normal);

            scope.RectTransform.anchoredPosition = new Vector2(200f, -150f);
            normal.RectTransform.anchoredPosition = new Vector2(700f, -400f);
            Canvas.ForceUpdateCanvases();
            yield return null;

            // Act : Scope の Body スポットへ運ぶ
            DragTo(normal, scope.GetFirstSection().Body.Spot.DropPosition);

            // Assert
            Assert.That(normal.IsRootBlock(), Is.False);
            Assert.That(normal.GetParentBlock(), Is.EqualTo(scope));
            Assert.That(scope.GetAllChildBlocksCount(containSelf: true), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator 接続先が無い場所へドロップするとブロックが破棄される() {
            // [NOTE] スポットの当たり範囲の外へ出すと破棄される仕様．
            //        テスト環境では ProgrammingEnv 自体にスポットが無いため、どこへ落としても破棄になる．
            var block = _env.CreateBlock(PrefabName.Normal);
            yield return null;

            DragTo(block, new Vector2(9999f, 9999f));
            yield return null;

            Assert.That(_env.GetRootBlocks(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator 接続後にドラッグするとセクションから切り離される() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var normal = _env.CreateBlock(PrefabName.Normal);
            scope.GetFirstSection().Body.AppendLast(normal);
            yield return null;

            Assert.That(normal.IsRootBlock(), Is.False, "前提: 接続済みであること");

            var drag = normal.RectTransform.GetComponent<BPG_BlockDraggingBase>();
            ((IBeginDragHandler)drag).OnBeginDrag(PointerAt(normal.RectTransform.position));
            yield return null;

            Assert.That(normal.RectTransform.parent, Is.EqualTo(_env.DraggingLayer));
            Assert.That(scope.GetAllChildBlocksCount(containSelf: true), Is.EqualTo(1));
        }
    }
}
