using UnityEngine;

namespace nitou.BlockPG.Events
{
    public partial class BPG_EventManager
    {

        /// <summary>
        /// <see cref="Input"/>クラスを利用してタッチ入力を検知する
        /// </summary>
        public class TouchInputHandler : IInputHandler
        {
            // references
            private BPG_EventManager _inputManager;

            public float HoldTime
            {
                get => _inputManager._holdTime;
                set => _inputManager._holdTime = value;
            }

            /// <summary>
            /// 最後に記録したポインター位置．
            /// </summary>
            public Vector2 LastPosition
            {
                get => _inputManager._lastPosition;
                set => _inputManager._lastPosition = value;
            }


            /// ----------------------------------------------------------------------------
            // Public Method

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public TouchInputHandler(BPG_EventManager inputManager)
            {
                _inputManager = inputManager;
            }

            /// <summary>
            /// 更新処理
            /// </summary>
            public void OnUpdate()
            {
                if (Input.touchCount <= 0)
                    return;

                Touch touch = Input.GetTouch(0); // 最初のタッチを検知
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        BPG_InputEventBus.PublishPrimaryKeyDown();
                        break;

                    case TouchPhase.Ended:
                        BPG_InputEventBus.PublishPrimaryKeyUp();
                        HoldTime = 0;
                        break;

                    case TouchPhase.Moved:

                        // ドラッグ判定
                        float distance = Vector2.Distance(LastPosition, touch.position);
                        if (distance > _inputManager.DRAG_THRESHOLD)
                        {
                            BPG_InputEventBus.PublishDrag();
                        }
                        break;

                    case TouchPhase.Stationary:
                        HoldTime += Time.deltaTime;
                        if (HoldTime > 0.6f)
                        {
                            BPG_InputEventBus.PublishPrimaryKeyHold();
                            HoldTime = 0;
                        }
                        break;
                }

                LastPosition = touch.position;
            }
        }
    }
}