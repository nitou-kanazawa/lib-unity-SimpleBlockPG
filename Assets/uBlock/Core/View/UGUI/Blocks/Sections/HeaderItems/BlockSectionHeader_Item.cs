using UnityEngine;

namespace Nitou.uBlock.View.UGUI {

    public class BlockSectionHeader_Item : ComponentBase ,
        IBlockSectionHeaderItem {

        /// <summary>
        /// サイズ情報．
        /// </summary>
        public Vector2 Size => RectTransform.sizeDelta;
    }

}
