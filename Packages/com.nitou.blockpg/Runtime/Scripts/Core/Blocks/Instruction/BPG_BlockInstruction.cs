using UnityEngine;

namespace nitou.BlockPG.Blocks.Instruction {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// ブロックの機能実装の基底クラス．
    /// </summary>
    /// <remarks>
    /// ブロックと同じ<see cref="GameObject"/>に付けて使う．
    /// 実行の仕組みは利用側が決めるため、ここでは所属ブロックの解決だけを担う．
    ///
    /// [NOTE] 抽象クラスにしているのは、そのまま付けても何もしないコンポーネントを
    ///        付けられる状態にしないため．
    /// </remarks>
    [DisallowMultipleComponent]
    public abstract class BPG_BlockInstruction : MonoBehaviour, I_BPG_Instruction {

        private I_BPG_Block _block;

        /// <summary>
        /// 紐づくブロック．
        /// </summary>
        public I_BPG_Block Block {
            get {
                if (_block == null) {
                    _block = GetComponent<I_BPG_Block>();
                }
                return _block;
            }
        }
    }
}
