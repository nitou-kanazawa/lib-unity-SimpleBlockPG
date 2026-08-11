using UnityEngine;
using UnityEngine.UI;

namespace Nitou.uBlock.View.UGUI {
    
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class BlockSectionHeader : ComponentBase {

        private Image _image;
        private BlockSection _section;


        /// ----------------------------------------------------------------------------
        // Private Method

        private void GatherComponents() {
            _image = GetComponent<Image>();

            // parents
            _section = transform.parent.GetComponent<BlockSection>();
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        private void OnValidate() {
            GatherComponents();
        }
#endif
    }
}
