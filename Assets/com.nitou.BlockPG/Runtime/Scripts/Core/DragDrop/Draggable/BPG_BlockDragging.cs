using UnityEngine;
using UnityEngine.EventSystems;

namespace nitou.BlockPG.DragDrop{
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Events;

    public class BPG_BlockDragging : BPG_BlockDraggingBase {

        /// ----------------------------------------------------------------------------
        // Interface Method

        /// <summary>
        /// ドラッグ開始処理．
        /// </summary>
        public override void OnBegineDrag(PointerEventData eventData) {
            // Append to dagging layer
             _system.AssignToDraggingPanel(this);
        }

        /// <summary>
        /// ドラッグ処理．
        /// </summary>
        public override void OnDrag(PointerEventData eventData) { }

        /// <summary>
        /// ドラッグ終了処理．
        /// </summary>
        public override void OnEndDrag(PointerEventData eventData) {

            if (DropToRaycastedFreeSpot(eventData)) { 
                AdjustTransformPositionAndRotation();
            }
        }

    }
}
