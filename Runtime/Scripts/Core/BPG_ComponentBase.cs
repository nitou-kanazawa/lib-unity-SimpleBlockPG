using UnityEngine;

namespace nitou.BlockPG{

    public abstract class BPG_ComponentBase : MonoBehaviour{

        private RectTransform _rectTransform;

        /// <summary>
        /// RectTransform.
        /// </summary>
        public RectTransform RectTransform => (_rectTransform != null) ? _rectTransform : (_rectTransform = GetComponent<RectTransform>());
    }
}
