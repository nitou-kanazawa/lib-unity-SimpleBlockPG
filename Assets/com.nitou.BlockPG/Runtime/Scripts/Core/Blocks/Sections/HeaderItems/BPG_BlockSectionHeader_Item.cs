using UnityEngine;

namespace nitou.BlockPG.Blocks.Section {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// <see cref="BPG_BlockSectionHeader"/>直下に配置されるレイアウト要素．
    /// </summary>
    public sealed class BPG_BlockSectionHeader_Item : BPG_ComponentBase, 
        I_BPG_BlockSectionHeaderItem {

        /// <summary>
        /// サイズ情報．
        /// </summary>
        public Vector2 Size => RectTransform.sizeDelta;
    }
}
