using System;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using nitou.AssetLoader;

namespace nitou.BlockPG.DragDrop {
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Blocks;

    // 
    public class DraggingSystem : SingletonMonoBehaviour<DraggingSystem>{

        [SerializeField] Transform _draggingObjHolder;
        [SerializeField] BPG_GhostBlock _ghostBlock;

        [SerializeField] float _detectionDistance = 50;


        /// ----------------------------------------------------------------------------
        // Property

        public float DetectionDistance => _detectionDistance;

        public BPG_GhostBlock GhostBlock => _ghostBlock;


        /// ----------------------------------------------------------------------------
        // Public Method

        internal bool CanDrag(I_BPG_Draggable draggable) {
            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        public void AssignToDraggingPanel(I_BPG_Draggable draggable) {
            if (draggable.RectTransform.parent == _draggingObjHolder)
                return;

            // set as a parent
            draggable.RectTransform.SetParent(_draggingObjHolder, worldPositionStays: true);
        }

        /// <summary>
        /// Returns the first spot component (used to place draggable components at) at the position
        /// </summary>
        public I_BPG_Spot DetectSpotAtPointerPosition(PointerEventData eventData, bool onlyTop = false) {

            I_BPG_Spot spot = null;

            var results = ListPool<RaycastResult>.Get();
            EventSystem.current.RaycastAll(eventData, results);

            // fidn spot object
            foreach (var result in results) {
                spot = result.gameObject.GetComponent<I_BPG_Spot>();

                // until the condition is satisfied
                if (onlyTop || spot != null) {
                    break;
                }
            }

            ListPool<RaycastResult>.Release(results);
            
            return spot;
        }

        /// <summary>
        /// 指定された距離内で、最も近いSpotを探します。
        /// </summary>
        public I_BPG_Spot FindClosestBlockSpot(I_BPG_Draggable draggable, float maxDistance) {
            return FindClosestSpot(draggable, maxDistance, spot =>
                // Block body 、
                (spot is BPG_SpotBlockBody ||
                // または対象ブロックが親を持っている
                (spot is BPG_SpotOuterArea && spot.Block?.ParentSection != null))
            );
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// Spotを探す共通メソッド.
        /// </summary>
        private I_BPG_Spot FindClosestSpot(I_BPG_Draggable draggable, float maxDistance, Func<I_BPG_Spot, bool> condition) {
            I_BPG_Spot foundSpot = null;
            float minDistance = Mathf.Infinity;

            // Find from spot list
            var targetSpots = BPG_Spot.ActiveSpots.Where(s => condition(s) && s.RectTransform.gameObject.activeSelf);
            foreach (var spot in targetSpots) {
                var d = spot.RectTransform.GetComponentInParent<I_BPG_Draggable>();
                if (d == null) continue;

                var programmingEnv = d.RectTransform.GetComponentInParent<I_BPG_ProgrammingEnv>();

                if (d != draggable) {
                    // 距離判定
                    float distance = Vector2.Distance(draggable.RayPoint, spot.DropPosition);
                    if (distance < minDistance && distance <= maxDistance) {
                        foundSpot = spot;
                        minDistance = distance;
                    }
                }
            }

            return foundSpot;
        }
    }

}