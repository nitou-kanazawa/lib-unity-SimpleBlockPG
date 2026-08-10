using UnityEngine;

namespace Nitou.uBlock {

    public interface IBlockSectionHeaderItem {

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
