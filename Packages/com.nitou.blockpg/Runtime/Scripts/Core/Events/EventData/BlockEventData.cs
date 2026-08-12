using System;
using UnityEngine;

namespace nitou.BlockPG.Events{
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.DragDrop;

    /// <summary>
    /// 選択イベントデータ．
    /// </summary>
    public struct BlockSelectEvent : IEquatable<BlockSelectEvent> {

        public I_BPG_Block Block { get;}

        public BlockSelectEvent(I_BPG_Block block) : this() {
            Block = block;
        }

        public override string ToString() {
            return $"[Select Block] {Block}";
        }

        public bool Equals(BlockSelectEvent other) {
            return Block.Equals(other.Block);
        }
    }


    /// <summary>
    /// ドラッグイベントデータ．
    /// </summary>
    public struct BlockLocationEvent : IEquatable<BlockLocationEvent> {

        public I_BPG_Block Block { get; }

        public BlockLocation Location { get; }

        public BlockLocationEvent(I_BPG_Block block, BlockLocation location) : this() {
            Block = block;
            Location = location;
        }

        public override string ToString() {
            return $"[Drag Block] {Block} : {Location}";
        }

        public bool Equals(BlockLocationEvent other) {
            return Location.Equals(other.Location) && Block.Equals(other.Block);
        }
    }


    /// <summary>
    /// 移動（ドラッグ＆ドロップ）イベントデータ．
    /// </summary>
    public struct BlockMoveEvent : IEquatable<BlockMoveEvent> {
        
        public I_BPG_Block Block { get; }

        public BlockLocation From { get; }

        public BlockLocation To { get; }


        public BlockMoveEvent(I_BPG_Block block, BlockLocation from, BlockLocation to) : this() {
            Block = block;
            From = from;
            To = to;
        }

        public override string ToString() {
            return $"[Move Block] {Block} : {From} => {To}";
        }

        public bool Equals(BlockMoveEvent other) {
            return From.Equals(other.From) && To.Equals(other.To)
                && Block.Equals(other.Block);
        }
    }

}
