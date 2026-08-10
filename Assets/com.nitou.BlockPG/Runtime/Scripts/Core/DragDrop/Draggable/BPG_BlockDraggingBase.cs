using nitou.BlockPG.Events;
using UnityEngine;
using UnityEngine.EventSystems;

namespace nitou.BlockPG.DragDrop {
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Blocks;

    [DisallowMultipleComponent]
    public class BPG_BlockDraggingBase : BPG_ComponentBase, I_BPG_Draggable,
        IPointerDownHandler,IDragHandler, IBeginDragHandler, IEndDragHandler {

        // reference
        protected DraggingSystem _system;

        // misc
        private Vector2 _offset;

        // [NOTE] Object.Destroy() はフレーム終了まで遅延されるため、破棄済みかどうかを
        //        Unityのnull判定で見分けられない．明示的にフラグで管理する．
        private bool _isRemoved = false;


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>
        /// Target block.
        /// </summary>
        public I_BPG_Block Block { get; private set; }

        /// <summary>
        /// Reference point for raycast.
        /// </summary>
        public Vector2 RayPoint => transform.position;

        /// <summary>
        /// 
        /// </summary>
        public bool IsDragging { get; private set; } = false;


        /// ----------------------------------------------------------------------------
        // Lifcyle Events

        // [NOTE] Unity は同名メッセージを最派生クラスの1つしか呼ばないため、
        //        派生側で OnEnable() を宣言せず、必ずこれを override すること．
        protected virtual void OnEnable() {
            Block = GetComponent<I_BPG_Block>();
            _system = DraggingSystem.Instance;
            if (Block is null) {
                Debug.LogWarning("Block is not attched.");
                this.enabled = false;
                return;
            }

        }


        /// ----------------------------------------------------------------------------
        #region Event handler

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
            OnPointerDown(eventData);
            BPG_BlockEventBus.PublishTouchEvent(Block);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
            _offset = RayPoint - eventData.position;
            _isRemoved = false;

            //
            if (_system.CanDrag(this)) {
                IsDragging = true;
                OnBegineDrag(eventData);
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData) {
            if (IsDragging) {
                // apply position
                RectTransform.position = eventData.position + _offset;
                OnDrag(eventData);
            }
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
            if (IsDragging) {
                OnEndDrag(eventData);
                IsDragging = false;
            }

            // Hide ghost block
            _system.GhostBlock.Hide();
            _system.GhostBlock.RectTransform.SetParent(null);

            // [NOTE] ドロップ先が見つからずブロックを破棄した場合、以降は破棄済みインスタンスの操作になる．
            if (_isRemoved)
                return;

            //
            this.Block.UpdateParentSection();
        }
        #endregion


        /// ----------------------------------------------------------------------------
        // Public Method

        public virtual void OnPointerDown(PointerEventData eventData) { }

        public virtual void OnBegineDrag(PointerEventData eventData) {}
        
        public virtual void OnDrag(PointerEventData eventData) { }

        public virtual void OnEndDrag(PointerEventData eventData) { }


        /// ----------------------------------------------------------------------------
        // Protected Method

        /// <summary>
        /// レイキャストで検出した空きスポットへドロップする．
        /// ドロップ先が見つからない場合はブロックを破棄し、falseを返す．
        /// </summary>
        protected bool DropToRaycastedFreeSpot(PointerEventData eventData) {

            // Get any spot
            var spot = _system.DetectSpotAtPointerPosition(eventData);
            
            if (spot != null) {
                // ProgramEnv取得
                var programmingEnv = spot.GetBelongedProgEnv();
                if (programmingEnv != null) {
                    programmingEnv.Append(this);
                    return true;
                }
            }

            // if can`t find any spot, remove block.
            _isRemoved = true;
            BPG_BlockUtils.RemoveBlock(Block);
            return false;
        }

        /// <summary>
        /// 位置姿勢を調整する．
        /// </summary>
        protected void AdjustTransformPositionAndRotation() {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            transform.localEulerAngles = Vector3.zero;
        }
    }
}
