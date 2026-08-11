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

        // ※つかんだ時点の配置場所．ドロップ先との比較に使う．
        private BlockLocation _locationOnBeginDrag = BlockLocation.Outside;


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
            if (Block is null) {
                Debug.LogWarning("Block is not attached.", this);
                this.enabled = false;
                return;
            }

            // [NOTE] シーン内に DraggingSystem が無いとドラッグ処理が一切成立しないため、
            //        取得できなければ自身を無効化する（各ハンドラでのnull参照を防ぐ）．
            _system = DraggingSystem.Instance;
            if (_system == null) {
                Debug.LogWarning($"{nameof(DraggingSystem)} is not found in the scene.", this);
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
            // つかんだ位置とブロック原点とのズレを保持する
            _offset = TryGetPointerWorldPoint(eventData, out var worldPoint)
                ? RayPoint - worldPoint
                : Vector2.zero;
            _isRemoved = false;

            //
            if (_system.CanDrag(this)) {
                // ※つかんだ時点の配置場所を、移動先との比較用に控える
                _locationOnBeginDrag = DraggingUtil.GetLocation(Block);
                BPG_BlockEventBus.PublishStartDragEvent(Block, _locationOnBeginDrag);

                IsDragging = true;
                OnBegineDrag(eventData);
            }
        }

        void IDragHandler.OnDrag(PointerEventData eventData) {
            if (IsDragging) {
                // apply position
                if (TryGetPointerWorldPoint(eventData, out var worldPoint)) {
                    var position = RectTransform.position;
                    position.x = worldPoint.x + _offset.x;
                    position.y = worldPoint.y + _offset.y;
                    RectTransform.position = position;
                }
                OnDrag(eventData);
            }
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
            bool wasDragging = IsDragging;
            if (IsDragging) {
                OnEndDrag(eventData);
                IsDragging = false;
            }

            // Hide ghost block
            // [NOTE] GhostBlock はインスペクタ未設定だとnullになる．
            if (_system.GhostBlock != null) {
                _system.GhostBlock.Hide();
                _system.GhostBlock.RectTransform.SetParent(null);
            }

            // [NOTE] ドロップ先が見つからずブロックを破棄した場合、以降は破棄済みインスタンスの操作になる．
            if (!_isRemoved) {
                this.Block.UpdateParentSection();
            }

            // ※破棄された場合は Outside として通知する
            if (wasDragging) {
                var location = _isRemoved ? BlockLocation.Outside : DraggingUtil.GetLocation(Block);
                BPG_BlockEventBus.PublishEndDragEvent(Block, _locationOnBeginDrag, location);
            }
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
        /// ポインター位置をワールド座標へ変換する．
        /// [NOTE] eventData.position はスクリーン座標のため、そのままワールド座標と
        ///        加減算できるのは CanvasのRenderModeが ScreenSpaceOverlay の場合のみ．
        ///        ScreenSpaceCamera / WorldSpace でもドラッグが破綻しないよう変換する．
        /// </summary>
        private bool TryGetPointerWorldPoint(PointerEventData eventData, out Vector2 worldPoint) {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    RectTransform, eventData.position, eventData.pressEventCamera, out var point)) {
                worldPoint = point;
                return true;
            }

            worldPoint = Vector2.zero;
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
