using System.Linq;
using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Events;

    /// <summary>
    /// 
    /// </summary>
    public static class BPG_BlockUtils
    {

        /// <summary>
        /// Remove block.
        /// </summary>
        public static void RemoveBlock(I_BPG_Block block)
        {
            if (block is null)
                return;

            // 子のブロックから逆順で破棄イベントを発火
            foreach (var childBlock in block.GetAllChildBlocks(containSelf: true).Reverse<I_BPG_Block>())
            {
                BPG_BlockEventBus.PublishDestroyEvent(childBlock);
            }

            // 破棄
            GameObject.Destroy(block.RectTransform.gameObject);
        }

    }

}