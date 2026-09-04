using System;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

namespace nitou.BlockPG.Events {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// ブロックへのポインタ操作を、プラットフォームによらない意味論イベントへ変換する．
    /// </summary>
    /// <remarks>
    /// uGUI の EventSystem がマウスとタッチを <see cref="PointerEventData"/> へ
    /// 正規化するため、そこに乗る限り入力デバイスの差は概ね吸収される．
    /// 自前で埋めるのは**長押し**と、**右クリックと長押しの統合**のみ．
    ///
    /// [NOTE] ドラッグ処理（<c>BPG_BlockDraggingBase</c>）とは分離している．
    ///        責務が異なるうえ、ドラッグ不可・クリック可のブロックも作れるようにするため．
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("BlockPG/Block Interaction")]
    public sealed class BPG_BlockInteraction : BPG_ComponentBase,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler {

        [Header("長押しと判定するまでの時間")]
        [SerializeField] private float _mouseHoldSeconds = 0.5f;
        [SerializeField] private float _touchHoldSeconds = 0.6f;

        private I_BPG_Block _block;

        private readonly Subject<BlockPointerEvent> _pressed = new();
        private readonly Subject<Unit> _released = new();

        // ※長押しが成立した押下では、離した時の主操作を発火させない
        private bool _consumedByHold;


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>マウスで長押しと判定するまでの時間．</summary>
        public float MouseHoldSeconds {
            get => _mouseHoldSeconds;
            set => _mouseHoldSeconds = Mathf.Max(0f, value);
        }

        /// <summary>タッチで長押しと判定するまでの時間．</summary>
        public float TouchHoldSeconds {
            get => _touchHoldSeconds;
            set => _touchHoldSeconds = Mathf.Max(0f, value);
        }


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        private void Awake() {
            _block = GetComponent<I_BPG_Block>();
            if (_block is null) {
                Debug.LogWarning("Block is not attached.", this);
                enabled = false;
                return;
            }

            // 長押しの検出
            // [NOTE] Update を使わないのは、ブロック数ぶんの毎フレーム処理を増やさないため．
            //        押下中のみタイマーが存在し、待機中のコストはゼロになる．
            //
            //        スケジューラを明示しているのは、UI の操作がゲーム内時間に
            //        引きずられないようにするため．ポーズ中（timeScale = 0）でも
            //        メニューを開けるべきである．
            //        （現行の UniRx では既定でも成立したが、既定への依存を避ける）
            _pressed
                .Select(e => Observable
                    .Timer(TimeSpan.FromSeconds(GetHoldSeconds(e.Source)), Scheduler.MainThreadIgnoreTimeScale)
                    .TakeUntil(_released)
                    .Select(_ => e))
                .Switch()               // ※新しい押下で前のタイマーを破棄する
                .Subscribe(OnHoldElapsed)
                .AddTo(this);
        }

        private void OnDestroy() {
            _pressed.Dispose();
            _released.Dispose();
        }


        /// ----------------------------------------------------------------------------
        #region Event handler

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
            _consumedByHold = false;

            // ※長押しの対象は主ボタンのみ．右クリックは押下時点で確定する
            if (eventData.button == PointerEventData.InputButton.Left) {
                _pressed.OnNext(BlockPointerEvent.From(_block, eventData));
            }
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
            _released.OnNext(Unit.Default);
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
            // ※指が動いたらドラッグへ移行する．移動量の判定は uGUI の閾値に任せる
            _released.OnNext(Unit.Default);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if (eventData.button == PointerEventData.InputButton.Right) {
                BPG_BlockEventBus.PublishSecondaryAction(BlockPointerEvent.From(_block, eventData));
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            // ※長押しで既に副次操作を発火している場合、離した時の主操作は抑制する
            if (_consumedByHold) {
                _consumedByHold = false;
                return;
            }

            var e = BlockPointerEvent.From(_block, eventData);
            if (e.ClickCount >= 2) {
                BPG_BlockEventBus.PublishDoubleAction(e);
            } else {
                BPG_BlockEventBus.PublishPrimaryAction(e);
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            // ※タッチでは押下時に乗ったことになるため、ホバーとしては扱わない
            if (BlockPointerEvent.GetSource(eventData) != PointerSource.Mouse)
                return;

            BPG_BlockEventBus.PublishHoverEnter(BlockPointerEvent.From(_block, eventData));
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
            if (BlockPointerEvent.GetSource(eventData) != PointerSource.Mouse)
                return;

            BPG_BlockEventBus.PublishHoverExit(BlockPointerEvent.From(_block, eventData));
        }
        #endregion


        /// ----------------------------------------------------------------------------
        // Private Method

        private float GetHoldSeconds(PointerSource source) {
            return source == PointerSource.Touch ? _touchHoldSeconds : _mouseHoldSeconds;
        }

        /// <summary>
        /// 長押しが成立した．
        /// </summary>
        /// <remarks>
        /// 指を離す前に発火する．押し続けた時点で反応があるのが一般的な作法のため．
        /// </remarks>
        private void OnHoldElapsed(BlockPointerEvent e) {
            _consumedByHold = true;
            BPG_BlockEventBus.PublishSecondaryAction(e);
        }
    }
}
