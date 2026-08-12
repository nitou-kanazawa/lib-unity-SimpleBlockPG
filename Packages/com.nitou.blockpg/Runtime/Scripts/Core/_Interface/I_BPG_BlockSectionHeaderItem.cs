using UnityEngine;

namespace nitou.BlockPG.Interface {

    public interface I_BPG_BlockSectionHeaderItem {

        /// <summary>
        /// 
        /// </summary>
        RectTransform RectTransform { get; }

        /// <summary>
        /// サイズ情報．
        /// </summary>
        Vector2 Size { get; }
    }
}