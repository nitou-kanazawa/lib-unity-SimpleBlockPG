using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace nitou.BlockPG.Interface{

    public interface I_BPG_BlockSectionHeader : ILayoutable{

        /// <summary>
        /// 
        /// </summary>
        IList<I_BPG_BlockSectionHeaderItem> Items { get; }

        /// <summary>
        /// ヘッダー直下の入力要素．（※<see cref="Items"/>のうち入力を伴うもの）
        /// </summary>
        IList<I_BPG_BlockSectionHeaderInput> Inputs { get; }

        /// <summary>
        /// Updates the ItemsArray with all the current I_BE2_BlockSectionHeaderItem (labels and inputs) in the header
        /// </summary>
        void UpdateItems();

        /// <summary>
        /// Updates the InputsArray with all the current I_BE2_BlockSectionHeaderInput (inputs only) in the header
        /// </summary>
        void UpdateInputs();
    }
}