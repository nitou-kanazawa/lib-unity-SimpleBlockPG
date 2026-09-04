using UnityEngine;
using UnityEngine.EventSystems;

namespace nitou.BlockPG.Interface {

    public interface IDraggable {

        void OnPointerDown(PointerEventData eventData);

        void OnBeginDragged(PointerEventData eventData);
        
        void OnDrag(PointerEventData eventData);
        
        void OnEndDrag(PointerEventData eventData);
    }

}