using System;
using UniRx;
using UnityEngine;

namespace nitou.BlockPG.Events {

    /// <summary>
    /// Event bus for within block programming modules.
    /// </summary>
    public static class BPG_InputEventBus
    {
        private static Subject<Unit> _onPrimaryKeyDown = new();
        private static Subject<Unit> _onPrimaryKeyHold = new();
        private static Subject<Unit> _onPrimaryKeyUp = new();
        private static Subject<Unit> _onPrimaryKeyUpEnd = new();
        private static Subject<Unit> _onSecondaryKeyDown = new();
        private static Subject<Unit> _onSecondaryKeyUp = new();
        private static Subject<Unit> _onDrag = new();

        // misc
        private static readonly bool debugMode = false;


        /// ----------------------------------------------------------------------------
        // Static Method

        /// <summary>
        /// 静的状態をリセットする．
        /// [NOTE] "Enter Play Mode Options" でドメインリロードを無効にした場合、
        ///        前回の実行で登録された購読が残るため明示的に破棄・再生成する．
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() {
            _onPrimaryKeyDown.Dispose();
            _onPrimaryKeyHold.Dispose();
            _onPrimaryKeyUp.Dispose();
            _onPrimaryKeyUpEnd.Dispose();
            _onSecondaryKeyDown.Dispose();
            _onSecondaryKeyUp.Dispose();
            _onDrag.Dispose();

            _onPrimaryKeyDown = new();
            _onPrimaryKeyHold = new();
            _onPrimaryKeyUp = new();
            _onPrimaryKeyUpEnd = new();
            _onSecondaryKeyDown = new();
            _onSecondaryKeyUp = new();
            _onDrag = new();
        }


        /// ----------------------------------------------------------------------------
        // Property

        /// <summary>
        /// メインキーが押下された時に通知するObservable．
        /// </summary>
        public static IObservable<Unit> OnPrimaryKeyDown => _onPrimaryKeyDown;

        /// <summary>
        /// メインキーが押下され続けている時に通知するObservable．
        /// </summary>
        public static IObservable<Unit> OnPrimaryKeyHold => _onPrimaryKeyHold;

        /// <summary>
        /// メインキーが離された時に通知するObservable．
        /// </summary>
        public static IObservable<Unit> OnPrimaryKeyUp => _onPrimaryKeyUp;

        /// <summary>
        /// メインキーが離された時の処理が終わった時に通知するObservable．
        /// </summary>
        public static IObservable<Unit> OnPrimaryKeyUpEnd => _onPrimaryKeyUpEnd;

        /// <summary>
        /// セカンダリキーが押下された時に通知するObservable．
        /// </summary>
        public static IObservable<Unit> OnSecondaryKeyDown => _onSecondaryKeyDown;

        /// <summary>
        /// セカンダリキーが離された時に通知するObservable．
        /// </summary>
        public static IObservable<Unit> OnSecondaryKeyUp => _onSecondaryKeyUp;

        /// <summary>
        /// ドラッグ操作中に通知するObservable．
        /// </summary>
        public static IObservable<Unit> OnDrag => _onDrag;


        /// ----------------------------------------------------------------------------
        // Internal Methods

        /// <summary>
        /// メインキーが押下されたイベントを発行します．
        /// </summary>
        internal static void PublishPrimaryKeyDown() => _onPrimaryKeyDown.OnNext(Unit.Default);

        /// <summary>
        /// メインキーが押下され続けているイベントを発行します．
        /// </summary>
        internal static void PublishPrimaryKeyHold() => _onPrimaryKeyHold.OnNext(Unit.Default);

        /// <summary>
        /// メインキーが離されたイベントを発行します．
        /// </summary>
        internal static void PublishPrimaryKeyUp() => _onPrimaryKeyUp.OnNext(Unit.Default);

        /// <summary>
        /// メインキーが離されたイベントを発行します．
        /// </summary>
        internal static void PublishPrimaryKeyUpEnd() => _onPrimaryKeyUpEnd.OnNext(Unit.Default);

        /// <summary>
        /// セカンダリキーが押下されたイベントを発行します．
        /// </summary>
        internal static void PublishSecondaryKeyDown() => _onSecondaryKeyDown.OnNext(Unit.Default);

        /// <summary>
        /// セカンダリキーが離されたイベントを発行します．
        /// </summary>
        internal static void PublishSecondaryKeyUp() => _onSecondaryKeyUp.OnNext(Unit.Default);

        /// <summary>
        /// ドラッグ操作中のイベントを発行します．
        /// </summary>
        internal static void PublishDrag() => _onDrag.OnNext(Unit.Default);
    }
}