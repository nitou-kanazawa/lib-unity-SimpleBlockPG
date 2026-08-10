using System.Collections.Generic;
using UnityEngine;

namespace nitou.BlockPG.Interface{

    public interface I_BPG_BlockSectionBody : ILayoutable {
        
        /// <summary>
        /// 所属セクション．
        /// </summary>
        I_BPG_BlockSection BlockSection { get; }
        
        /// <summary>
        /// 接続されている子ブロックのリスト．
        /// </summary>
        IReadOnlyList<I_BPG_Block> ChildBlocks { get; }
        
        /// <summary>
        /// ブロック接続の可否判定用コンポーネント．
        /// </summary>
        I_BPG_Spot Spot { get;}

        /// <summary>
        /// Updates ChildBlocksCount and ChildBlocksArray with the current child blocks.
        /// </summary>
        void UpdateChildBlocks();
    }


    /// <summary>
    /// <see cref="I_BPG_BlockSectionBody"/>型の汎用的な拡張メソッド集．
    /// </summary>
    public static class BPG_BlockSectionBody_Extensions {

        /// <summary>
        /// セクションに対象ブロックを接続する．
        /// </summary>
        public static void Append(this I_BPG_BlockSectionBody self, I_BPG_Block block, int siblingIndex = 0) {
            block.RectTransform.SetParent(self.Spot.RectTransform);
            block.RectTransform.SetSiblingIndex(siblingIndex);
            
            // 子ブロック側の更新
            block.SetParentSection(self.BlockSection);

            // セクション側の更新
            self.BlockSection.UpdateLayout();

            Debug.Log($"Connect to {self.BlockSection.Block.RectTransform.name} [{block.RectTransform.GetSiblingIndex()}]");
        }

        /// <summary>
        /// セクションに対象ブロックを接続する．
        /// </summary>
        public static void AppendFirst(this I_BPG_BlockSectionBody self, I_BPG_Block block) {
            int siblingIndex = 0;
            self.Append(block, siblingIndex);
        }

        /// <summary>
        /// セクションに対象ブロックを接続する．
        /// </summary>
        public static void AppendLast(this I_BPG_BlockSectionBody self, I_BPG_Block block) {
            int siblingIndex = self.RectTransform.childCount;
            self.Append(block, siblingIndex);
        }
    }

}