using UnityEngine;

namespace Nitou.uBlock {

    public abstract class ComponentBase : MonoBehaviour {

        private RectTransform _rectTransform;

        /// <summary>
        /// RectTransform.
        /// </summary>
        public RectTransform RectTransform => (_rectTransform != null) ? _rectTransform : (_rectTransform = GetComponent<RectTransform>());

    }
}
