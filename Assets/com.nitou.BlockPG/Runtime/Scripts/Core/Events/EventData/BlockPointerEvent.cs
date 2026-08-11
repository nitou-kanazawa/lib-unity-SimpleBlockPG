using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace nitou.BlockPG.Events {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// 操作の発生源．
    /// </summary>
    public enum PointerSource {

        /// <summary>マウス・ペンなどの間接ポインタ．</summary>
        Mouse,

        /// <summary>画面への直接タッチ．</summary>
        Touch,
    }


    /// <summary>
    /// ブロックへのポインタ操作．
    /// </summary>
    /// <remarks>
    /// [NOTE] uGUI の <see cref="PointerEventData"/> は使い回されるため、
    ///        購読側へ渡す前に必要な値をここへ写し取る．
    ///        遅延して参照すると別の操作の値に書き換わっている．
    /// </remarks>
    public readonly struct BlockPointerEvent : IEquatable<BlockPointerEvent> {

        /// <summary>操作対象のブロック．</summary>
        public I_BPG_Block Block { get; }

        /// <summary>操作が起きたスクリーン座標．</summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>操作の発生源．</summary>
        public PointerSource Source { get; }

        /// <summary>連続して押された回数．（※単発の場合は1）</summary>
        public int ClickCount { get; }


        public BlockPointerEvent(I_BPG_Block block, Vector2 screenPosition,
            PointerSource source, int clickCount = 1) {
            Block = block;
            ScreenPosition = screenPosition;
            Source = source;
            ClickCount = clickCount;
        }

        /// <summary>
        /// uGUI のイベントデータから写し取る．
        /// </summary>
        public static BlockPointerEvent From(I_BPG_Block block, PointerEventData eventData) {
            return new BlockPointerEvent(
                block,
                eventData.position,
                GetSource(eventData),
                Mathf.Max(1, eventData.clickCount));
        }

        /// <summary>
        /// 発生源を判定する．
        /// </summary>
        /// <remarks>
        /// [NOTE] Unity はタッチに 0 以上の pointerId（指の番号）を、
        ///        マウスのボタンに負の値を割り当てる．この規約に従って判定する．
        ///        入力モジュールの実装に依存しないよう、型ではなく id で見ている．
        /// </remarks>
        public static PointerSource GetSource(PointerEventData eventData) {
            return eventData.pointerId >= 0 ? PointerSource.Touch : PointerSource.Mouse;
        }

        public override string ToString() {
            return $"[{Source}] {Block} at {ScreenPosition} x{ClickCount}";
        }

        public bool Equals(BlockPointerEvent other) {
            return Equals(Block, other.Block)
                && ScreenPosition.Equals(other.ScreenPosition)
                && Source == other.Source
                && ClickCount == other.ClickCount;
        }
    }
}
