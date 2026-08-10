using UnityEngine;

namespace nitou.BlockPG.Environments{
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.DragDrop;

    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(BPG_SpotProgrammingEnv))]
    public class BPG_ProgrammingEnv : BPG_ComponentBase, I_BPG_ProgrammingEnv {

        private CanvasGroup _canvasGroup;
        

    }
}
