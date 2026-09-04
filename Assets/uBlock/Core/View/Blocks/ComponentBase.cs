using UnityEngine;

namespace Nitou.BlockPG.View {

    public abstract class ComponentBase : MonoBehaviour {

        private RectTransform _rectTransform;

        /// <summary>
        /// RectTransform.
        /// </summary>
        public RectTransform RectTransform => (_rectTransform != null) ? _rectTransform : (_rectTransform = GetComponent<RectTransform>());

    }
}
