using UnityEngine;

namespace nitou.BlockPG.DragDrop{
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Blocks;
    using UnityEngine.EventSystems;

    public class BPG_BlockDraggingWithConnection : BPG_BlockDraggingBase {

        private I_BPG_Spot _currentSpot;


        /// ----------------------------------------------------------------------------
        // Interface Method

        public override void OnBegineDrag(PointerEventData eventData) {
            // append to dagging layer
            _system.AssignToDraggingPanel(this);
        }

        public override void OnDrag(PointerEventData eventData) {
            // detect candidate spot
            DetectConectableBlockSpot();
        }

        public override void OnEndDrag(PointerEventData eventData) {

            // 接続対象のブロックが存在する場合、
            if (_currentSpot != null) {
                HandleDropToCurrentSpot();
                AdjustTransformPositionAndRotation();
            }
            // 接続対象のブロックが存在しない場合、
            // [NOTE] ドロップ先が無いとブロックは破棄されるため、成功時のみ位置を調整する．
            else if (DropToRaycastedFreeSpot(eventData)) {
                AdjustTransformPositionAndRotation();
            }

            _currentSpot = null;
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// 現在のポインター座標での<see cref="I_BE2_Spot">スポット</see>を検出する
        /// ※<see cref="BE2_DragDropManager"/>によってフレーム終了時に実行される
        /// </summary>
        private void DetectConectableBlockSpot() {
            
            // 
            var spot = _system.FindClosestBlockSpot(this, _system.DetectionDistance);

            //// 接続可否判定のデモコード
            //if (spot != null && spot.Block != null && this.Block.IsConditionBlock()) {
            //    bool canConnect = true;

            //    // Conditionブロック入れ子にすることは出来ない
            //    if (spot.Block.IsConditionBlock() && spot is not BE2_SpotOuterArea) {
            //        canConnect = false;
            //    }

            //    // 
            //    else if (spot.Block.ParentSection != null && spot.Block.ParentSection.Block.IsConditionBlock()) {
            //        canConnect = false;
            //    }

            //    // 接続不可の場合,
            //    if (!canConnect) {
            //        _dragDropManager.GhostBlock.Hide();
            //        _dragDropManager.SetCurrentSpot(null);
            //        return;
            //    }
            //}

            // BlockBody
            if (spot is BPG_SpotBlockBody && spot.Block != this.Block) {
                _system.GhostBlock.Show(
                    parent: spot.RectTransform,
                    localScale: Vector3.one,
                    siblingIndex: 0);

                // cache spot
                _currentSpot = spot;
            }
            // OuterArea (ブロック下部)
            else if (spot is BPG_SpotOuterArea) {
                _system.GhostBlock.Show(
                    parent: spot.Block.RectTransform.parent,
                    localScale: Vector3.one,
                    siblingIndex: spot.Block.RectTransform.GetSiblingIndex() + 1);  // ※対象ブロックの一つ下に配置する

                // 
                spot.Block.ParentSection.UpdateLayout();

                // cache spot
                _currentSpot = spot;
            }
            // その他
            else {
                _system.GhostBlock.Hide();
                _currentSpot = null;
            }

            // Debug.Log($"current spot is null : {_dragDropManager.CurrentSpot is null}");
        }

        /// <summary>
        /// Spotに配置する
        /// </summary>
        private void HandleDropToCurrentSpot() {
            var spot = _currentSpot;

            // block body
            if (spot is BPG_SpotBlockBody spotBlockBody) {
                this.Block.ConnectTo(spotBlockBody);
            } 
            // block outer area
            else if(spot is BPG_SpotOuterArea spotOuterArea) {
                this.Block.InsertNextTo(spotOuterArea);
            }

            _currentSpot = null;
        }
    }
}
