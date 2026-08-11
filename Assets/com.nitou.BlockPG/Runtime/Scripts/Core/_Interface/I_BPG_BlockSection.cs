using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace nitou.BlockPG.Interface{

    public interface I_BPG_BlockSection : ILayoutable {

        /// <summary>
        /// 積み上げ方向．（※所属ブロックのレイアウトに従う）
        /// </summary>
        Blocks.BlockLayoutAxis Axis { get; }

        I_BPG_Block Block { get; }

        I_BPG_BlockSectionHeader Header { get; }

        I_BPG_BlockSectionBody Body { get; }

        /// <summary>
        /// 折り畳まれているかどうか．
        /// </summary>
        bool IsCollapsed { get; }

        /// <summary>
        /// 折り畳み状態を設定する．
        /// </summary>
        /// <remarks>
        /// ボディを非表示にすることで実現する．子ブロックは保持されたままなので、
        /// 畳んだ状態で保存しても中身は失われない．
        /// ボディを持たないセクションでは何も起こらない．
        /// </remarks>
        void SetCollapsed(bool collapsed);
    }


    /// <summary>
    /// Basic extension methods for type of <see cref="I_BPG_BlockSection"/>.
    /// </summary>
    public static class BPG_BlockSection_Extensions {

        // ----------------------------------------------------------------------------
        #region Info

        /// <summary>
        /// 
        /// </summary>
        public static int GetSectionIndex(this I_BPG_BlockSection self) {
            return self.RectTransform.GetSiblingIndex();
        }

        #endregion


        /// ----------------------------------------------------------------------------
        #region Getter

        /// <summary>
        /// Body直下のブロックを取得する
        /// </summary>
        public static IEnumerable<I_BPG_Block> GetBodyBlocks(this I_BPG_BlockSection self) {
            return self.Body.ChildBlocks.Where(b => b != null);
        }

        /// <summary>
        /// 子階層以下の<see cref="I_BPG_Block"/>を再帰的に取得する
        /// </summary>
        public static List<I_BPG_Block> GetAllChaildBlocks(this I_BPG_BlockSection self) {
            if (self.Body == null) return new List<I_BPG_Block>();

            // 子階層以下
            return self.GetBodyBlocks()
                .SelectMany(block => block.GetAllChaildBlocks())
                .ToList();
        }

        /// <summary>
        /// 子階層以下の<see cref="I_BPG_Block"/>の数を取得する
        /// </summary>
        public static int GetAllChaildBlocksCount(this I_BPG_BlockSection self) {
            if (self == null || self.Body == null) return 0;

            // Blockの総数
            return self.GetBodyBlocks()
                .Select(block => block.GetAllChaildBlocksCount())
                .Sum();
        }


        /// <summary>
        /// Obtains a reference to the parent section to which it belongs.
        /// </summary>
        //public static I_BPG_Block GetParentBlock(this I_BPG_BlockSection self) {
        //    return (self.ParentSection != null) ? self.ParentSection.Block : null;
        //}

        #endregion
    }
}
