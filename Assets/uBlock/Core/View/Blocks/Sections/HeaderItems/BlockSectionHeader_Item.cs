using UnityEngine;

namespace Nitou.BlockPG.View {

    public class BlockSectionHeader_Item : ComponentBase ,
        IBlockSectionHeaderItem {

        /// <summary>
        /// サイズ情報．
        /// </summary>
        public Vector2 Size => RectTransform.sizeDelta;
    }

}
