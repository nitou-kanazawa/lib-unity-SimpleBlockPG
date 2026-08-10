using UnityEngine;

namespace nitou.BlockPG.Events {

    public partial class BPG_EventManager {

        /// <summary>
        /// <see cref="Input"/>クラスを利用してキーボード入力を検知する
        /// </summary>
        public class KeyboardInputHandler : IInputHandler {

            public KeyCode primaryKey = KeyCode.Mouse0;
            public KeyCode secondaryKey = KeyCode.Mouse1;
            public KeyCode deleteKey = KeyCode.Delete;
            public KeyCode auxKey0 = KeyCode.LeftControl;

            // references
            private BPG_EventManager _inputManager;

            public float HoldTime {
                get => _inputManager._holdTime;
                set => _inputManager._holdTime = value;
            }

            /// <summary>
            /// 最後に記録したポインター位置．
            /// </summary>
            public Vector2 LastPosition {
                get => _inputManager._lastPosition;
                set => _inputManager._lastPosition = value;
            }


            /// ----------------------------------------------------------------------------
            // Public Method

            /// <summary>
            /// Constructor.
            /// </summary>
            public KeyboardInputHandler(BPG_EventManager inputManager) {
                _inputManager = inputManager;
            }

            /// <summary>
            /// Update process.
            /// </summary>
            public void OnUpdate() {
                // Primary Key (Up/Down)
                if (Input.GetKeyDown(primaryKey)) {
                    BPG_InputEventBus.PublishPrimaryKeyDown();
                }
                if (Input.GetKeyUp(primaryKey)) {
                    BPG_InputEventBus.PublishPrimaryKeyUp();
                    HoldTime = 0f;
                }

                // Primary Key (Hold)
                //if (_dragDropManager.CurrentDrag != null && !_dragDropManager.IsDragging) {
                //    HoldTime += Time.deltaTime;
                //    if (HoldTime > 0.6f) {
                //        BPG_InputEventBus.PublishPrimaryKeyHold();
                //        HoldTime = 0;
                //    }
                //}

                // Primary Key (Drag)
                if (Input.GetKey(primaryKey)) {

                    // ドラッグ判定
                    var screenPos = _inputManager.ScreenPointerPosition;
                    float distance = Vector2.Distance(LastPosition, (Vector2)screenPos);
                    if (distance > _inputManager.DRAG_THRESHOLD) {
                        BPG_InputEventBus.PublishDrag();
                    }
                }

                // Secondary Key (Up/Down)
                {
                    if (Input.GetKeyDown(secondaryKey)) {
                        BPG_InputEventBus.PublishSecondaryKeyDown();
                    }
                    if (Input.GetKeyUp(secondaryKey)) {
                        BPG_InputEventBus.PublishSecondaryKeyUp();
                    }
                }

                LastPosition = _inputManager.ScreenPointerPosition;
            }
        }
    }

}