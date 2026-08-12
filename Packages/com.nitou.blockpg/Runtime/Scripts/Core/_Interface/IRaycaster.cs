using UnityEngine;

namespace nitou.BlockPG.Interface {
    using nitou.BlockPG.DragDrop;

    public interface IRaycaster{

        /// <summary>
        /// Returns the first draggable component at the position 
        /// </summary>
        I_BPG_Draggable GetDragAtPosition(Vector2 screenPosition);

        /// <summary>
        /// Returns the first spot component (used to place draggable components at) at the position.
        /// </summary>
        I_BPG_Spot GetSpotAtPosition(Vector3 screenPosition);

        /// <summary>
        /// Returns the first spot component of an specific type (used to place draggable components at) that is closer to the given draggable and inside the range.
        /// </summary>
        I_BPG_Spot FindClosestSpotOfType<T>(I_BPG_Draggable drag, float maxDistance) where T : I_BPG_Spot;

        /// <summary>
        /// Returns the first spot component of types BE2_SpotBlockBody or BE2_SpotOuterArea (used to place draggable components at) that is closer to the given draggable and inside the range.
        /// </summary>
        I_BPG_Spot FindClosestSpotForBlock(I_BPG_Draggable drag, float maxDistance);
    }
}
