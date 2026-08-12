using UnityEngine;

namespace nitou.BlockPG.Blocks {

    /// <summary>
    /// セクションを左から右へ並べるレイアウト．（※式を組み立てるブロック）
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("BlockPG/Block Horizontal Layout")]
    public sealed class BPG_BlockHorizontalLayout : BPG_BlockLayoutBase {

        public override BlockLayoutAxis Axis => BlockLayoutAxis.Horizontal;
    }
}
