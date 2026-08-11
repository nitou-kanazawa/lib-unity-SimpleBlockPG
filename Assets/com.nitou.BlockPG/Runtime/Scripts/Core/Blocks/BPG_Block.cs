using UniRx;
using UnityEngine;

namespace nitou.BlockPG.Blocks{
    using nitou.BlockPG.Events;

    public sealed class BPG_Block : BPG_BlockBase {

        [SerializeField] BlockType _type;

        /// <summary>
        /// Classification of blocks.
        /// </summary>
        public override BlockType Type => _type;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        protected override void OnInitialize() {
            GatherComponents();
        }

    }
}
