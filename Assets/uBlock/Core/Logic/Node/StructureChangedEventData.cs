using System;

namespace Nitou.uBlock {
    
    public enum TreeNodeChangedAction {
        /// <summary>Added within the tree structure spreading from a specific node.</summary>
        Join,
        /// <summary>Moved within the tree structure spreading from a specific node.</summary>
        Move,
        /// <summary>Removed from the tree structure spreading from a specific node.</summary>
        Deviate,
    }


    public struct StructureChangedEventData {


    }
}
