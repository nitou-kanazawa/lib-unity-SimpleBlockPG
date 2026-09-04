using UnityEngine;

namespace nitou.BlockPG.Blocks {

    /// <summary>
    /// セクションを上から下へ積むレイアウト．（※文を並べるブロック）
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("BlockPG/Block Vertical Layout")]
    public sealed class BPG_BlockVerticalLayout : BPG_BlockLayoutBase {

        public override BlockLayoutAxis Axis => BlockLayoutAxis.Vertical;
    }
}
