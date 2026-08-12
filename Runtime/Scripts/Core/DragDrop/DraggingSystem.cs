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

        /// <summary>
        /// 接続先とみなす最大距離．
        /// </summary>
        public float DetectionDistance => _detectionDistance;

        /// <summary>
        /// 接続先の予告表示．（※未設定でも接続そのものは成立する）
        /// </summary>
        public BPG_GhostBlock GhostBlock => _ghostBlock;

        /// <summary>
        /// ドラッグ中のブロックの一時的な配置先．
        /// </summary>
        public Transform DraggingHolder => _draggingObjHolder;


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 実行時に参照を設定する．
        /// </summary>
        /// <remarks>
        /// シーンを介さずにワークスペースを組み立てる場合に使う．
        /// インスペクタで設定済みの場合は、このメソッドを呼ぶ必要はない．
        ///
        /// <paramref name="draggingHolder"/> が未設定だと、ドラッグしたブロックが
        /// 親なしへ飛ばされて画面から消えるため、ドラッグ処理が成立しない．
        /// </remarks>
        /// <param name="draggingHolder">ドラッグ中のブロックの一時的な配置先．</param>
        /// <param name="ghostBlock">接続先の予告表示．null なら予告なしで動作する．</param>
        /// <param name="detectionDistance">接続先とみなす最大距離．null なら現在値を保つ．</param>
        public void Setup(Transform draggingHolder, BPG_GhostBlock ghostBlock = null,
            float? detectionDistance = null) {

            if (draggingHolder == null)
                throw new System.ArgumentNullException(nameof(draggingHolder));

            _draggingObjHolder = draggingHolder;
            _ghostBlock = ghostBlock;

            if (detectionDistance.HasValue) {
                _detectionDistance = Mathf.Max(0f, detectionDistance.Value);
            }
        }

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