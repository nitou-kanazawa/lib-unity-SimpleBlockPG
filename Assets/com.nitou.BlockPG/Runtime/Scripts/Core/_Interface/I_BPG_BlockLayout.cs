using System.Collections.Generic;
using UnityEngine;

namespace nitou.BlockPG.Interface{

    /// <summary>
    /// 
    /// </summary>
    public interface I_BPG_BlockLayout : ILayoutable{

        /// <summary>
        /// 子セクション．
        /// </summary>
        IReadOnlyList<I_BPG_BlockSection> Sections { get; }

        /// <summary>
        /// ブロックカラー．.
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        /// レイアウトの再計算を予約する．
        /// 予約は祖先ブロックへも伝播し、次のフレームでまとめて更新される．
        /// </summary>
        void SetLayoutDirty();
    }
}
