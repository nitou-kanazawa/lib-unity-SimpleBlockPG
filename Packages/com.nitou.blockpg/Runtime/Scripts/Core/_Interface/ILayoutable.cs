using UnityEngine;

namespace nitou.BlockPG.Interface{

    public interface ILayoutable {

        RectTransform RectTransform { get; }

        Vector2 Size { get; }

        //Shadow Shadow { get; }

        /// <summary>
        /// Updates the layout of an indivisual block section. Used to correctly resize the section after adding child and operation blocks
        /// </summary>
        void UpdateLayout();
    }
}
