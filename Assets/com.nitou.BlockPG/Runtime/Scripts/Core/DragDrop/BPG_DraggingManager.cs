using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;


namespace nitou.BlockPG.DragDrop
{
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Events;
    //using nitou.BlockPG.Enviorment;

    /// <summary>
    /// <see cref="I_BE2_ProgrammingEnv"/>内の<see cref="BE2_Block"/>群の構成変化を監視するコンポーネント．
    /// </summary>
    public sealed class BPG_DraggingManager : MonoBehaviour
    {
        [Header("Dragging")]
        [SerializeField] Transform _draggingObjParent;
        //[SerializeField] BE2_GhostBlock _ghostBlock;

        [Header("Parameters")]
        [SerializeField] float _detectionDistance = 40;

        // references
        private I_BPG_Config _config;

        // state
        private readonly BoolReactiveProperty _isDraggingRP = new(false);
        private readonly ReactiveProperty<I_BPG_Draggable> _currentDragRP = new(null);


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>
        /// 
        /// </summary>
        public IRaycaster Raycaster { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public float DetectionDistance => _detectionDistance;

        /// <summary>
        /// Current drag target.
        /// </summary>
        public I_BPG_Draggable CurrentDrag
        {
            get => _currentDragRP.Value;
            private set => _currentDragRP.Value = value;
        }

        /// <summary>
        /// Current drop spot．
        /// </summary>
        public I_BPG_Spot CurrentSpot { get; private set; }

        /// <summary>
        /// ブロックのドラッグ操作中かどうか
        /// </summary>
        public bool IsDragging
        {
            get => _isDraggingRP.Value;
            private set => _isDraggingRP.Value = value;
        }

        /// <summary>
        /// 環境内に入っているかどうか
        /// </summary>
        public bool IsOverapping { get; private set; }



        /*

        /// <summary>
        /// ゴーストブロック
        /// </summary>
        public I_BE2_GhostBlock GhostBlock => _ghostBlock;

        public Transform GhostBlockTransform => _ghostBlock.transform;

        public Transform DraggedObjectsTransform => _draggingObjParent;


        /// ----------------------------------------------------------------------------
        // Unity Lifecycle Event

        public void Initialize(I_BPG_Config config)
        {
            _config = config;
            Raycaster = GetComponent<IRaycaster>();

            // Observe input events
            BPG_InputEventBus.OnPrimaryKeyDown.Where(_ => _config.CanDrag)
                .Subscribe(_ => OnPointerDown())
                .AddTo(this);
            BPG_InputEventBus.OnPrimaryKeyHold.Where(_ => _config.CanDrag)
                .Subscribe(_ => OnPointerDownHold())
                .AddTo(this);
            BPG_InputEventBus.OnDrag.Where(_ => _config.CanDrag)
                .Subscribe(_ => OnDrag())
                .AddTo(this);
            BPG_InputEventBus.OnPrimaryKeyUp.Where(_ => IsDragging)
                .Subscribe(_ => OnPointerUp())
                .AddTo(this);
            BPG_InputEventBus.OnSecondaryKeyDown
                .Subscribe(_ => OnRightPointerDown())
                .AddTo(this);

            // Observe state changing
            _config.CurrentModeRP
                .Where(mode => mode is not EngineMode.Teaching && IsDragging)
                .Subscribe(_ =>OnPointerUp())
                .AddTo(this);
        }

        private void OnDestroy()
        {
            _isDraggingRP.Dispose();
            _currentDragRP.Dispose();
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 指定した<see cref="I_BE2_Drag"/>をDraggedParentの子に設定する
        /// </summary>
        public void AssignToDraggedParent(I_BE2_Drag dragable)
        {
            if (dragable.Transform.parent == _draggingObjParent)
                return;

            // 親として設定
            dragable.Transform.SetParent(_draggingObjParent, worldPositionStays: true);
        }

        public void SetCurrentDrag(I_BE2_Drag drag)
        {
            CurrentDrag = drag;
        }

        public void SetCurrentSpot(I_BE2_Spot spot)
        {
            CurrentSpot = spot;
        }


        /// ----------------------------------------------------------------------------
        // Private Method 

        private void OnPointerDown()
        {
            // 対象の取得
            var pointerPosition = BE2_InputManager.Instance.ScreenPointerPosition;
            I_BE2_Drag drag = Raycaster.GetDragAtPosition(pointerPosition);

            CurrentDrag = drag;
            CurrentDrag?.OnPointerDown();
        }

        private void OnPointerDownHold()
        {
            // 対象の取得
            var pointerPosition = BE2_InputManager.Instance.ScreenPointerPosition;
            I_BE2_Drag drag = Raycaster.GetDragAtPosition(pointerPosition);

            drag?.OnPointerDownHold();
        }

        private void OnDrag()
        {
            if (CurrentDrag is null)
                return;

            // v2.11.1 - added handler method to the BE2_DragDropManager.OnDrag for the new Block drag events
            if (!IsDragging)
            {
                StartCoroutine(HandleStartDragging(CurrentDrag.Block));
            }

            CurrentDrag.OnDrag();
            IsDragging = true;
        }

        private void OnPointerUp()
        {
            if (CurrentDrag != null)
            {
                CurrentDrag.OnPointerUp();
                StartCoroutine(HandleEndDragging(CurrentDrag.Block));
            }

            // reset references
            CurrentDrag = null;
            CurrentSpot = null;
            GhostBlock.Transform.SetParent(null);

            // reset flags
            IsDragging = false;
            IsOverapping = false;

            BE2_MainEventsManager.Instance.TriggerEvent(BE2EventTypes.OnPrimaryKeyUpEnd);
            BPG_InputEventBus.PublishPrimaryKeyUpEnd();
        }

        private void OnRightPointerDown()
        {
            // 対象の取得
            var pointerPosition = BE2_InputManager.Instance.ScreenPointerPosition;
            I_BE2_Drag drag = Raycaster.GetDragAtPosition(pointerPosition);

            drag?.OnRightPointerDown();
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private IEnumerator HandleStartDragging(I_BE2_Block block)
        {
            I_BE2_BlockSectionHeader parentHeader = null;
            bool existBlock = (block as Object) != null; // block が null でないか確認
            if (existBlock)
            {
                // 参照の更新
                block.Instruction.InstructionBase.BlocksStack = block.Transform.GetComponentInParent<I_BE2_BlocksStack>();
                block.ParentSection = block.Transform.GetComponentInParent<I_BE2_BlockSection>();
                parentHeader = block.Transform.parent.GetComponent<I_BE2_BlockSectionHeader>();
            }

            // フレーム終わりまで待機
            yield return new WaitForEndOfFrame();

            if (existBlock)
            {
                bool existParentHeader = parentHeader != null;
                bool existParentSection = block.ParentSection != null;

                // どこからドラッグ開始してるか判定
                var location = (existParentHeader, existParentSection) switch
                {
                    (true, false) => BlockLocation.InputSpot,
                    (false, false) => BlockLocation.ProgEnv,
                    (false, true) => BlockLocation.Stack,
                    _ => throw new System.NotImplementedException()
                };

                // イベント発火
                BPG_BlockEventBus.PublishStartDragEvent(block, location);
            }
            else
            {
                // イベント発火
                BPG_BlockEventBus.PublishStartDragEvent(null, BlockLocation.Outside);
            }

        }

        private IEnumerator HandleEndDragging(I_BE2_Block block)
        {
            // フレーム終わりまで待機
            yield return new WaitForEndOfFrame();

            bool existBlock = (block as Object) != null;    // ※(block != null) ではブロック破棄時にエラーが発生する
            if (existBlock)
            {
                // 参照の更新
                block.Instruction.InstructionBase.BlocksStack = block.Transform.GetComponentInParent<I_BE2_BlocksStack>();
                block.ParentSection = block.Transform.GetComponentInParent<I_BE2_BlockSection>();

                bool existParentHeader = block.Transform.parent.GetComponent<I_BE2_BlockSectionHeader>() != null;
                bool existParentSection = block.ParentSection != null;

                // ドロップ先に応じたイベントタイプを判定
                var location = (existParentHeader, existParentSection) switch
                {
                    (false, false) => BlockLocation.ProgEnv,    // 親セクションがない
                    (true, _) => BlockLocation.InputSpot,       // 親ヘッダーがある場合
                    _ => BlockLocation.Stack,                   // それ以外
                };

                // イベント発火
                BPG_BlockEventBus.PublishEndDragEvent(block, location);
            }
            else
            {
                // ブロックが存在しない場合の処理 (削除イベント)
                BPG_BlockEventBus.PublishEndDragEvent(block, BlockLocation.Outside);
            }

        }


        */

        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        private void Reset(){}
#endif
    }
}