using UniRx;
using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Events;

    /// <summary>
    /// A block instance that serves as the entry point for the program.
    /// </summary>
    public sealed class BPG_TriggerBlock : BPG_BlockBase {

        /// <summary>
        /// Classification of blocks.
        /// </summary>
        public override BlockType Type => BlockType.Trigger;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        // [NOTE] Awake() を直接宣言すると基底の Awake() が呼ばれなくなるため、OnInitialize() を override する．
        protected override void OnInitialize() {
            GatherComponents();
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// Set the active state of the Shadow.
        /// </summary>
        public override void SetShadowActive(bool isActive) { }
        //=>  this.SetShadow(isActive);

    }

}