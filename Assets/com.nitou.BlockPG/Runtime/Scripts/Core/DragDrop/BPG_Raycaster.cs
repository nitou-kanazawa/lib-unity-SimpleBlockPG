using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace nitou.BlockPG.DragDrop {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// 
    /// </summary>
    public sealed class BPG_Raycaster : MonoBehaviour {

        [SerializeField] List<GraphicRaycaster> _raycasters = new();

        public RenderMode CanvasRenderMode { get; private set; } = RenderMode.ScreenSpaceOverlay;

        public IReadOnlyList<GraphicRaycaster> Raycasters => _raycasters;


        /*

        /// ----------------------------------------------------------------------------
        // LifeCycle Events

        private void Start() {
            OnValidate();
        }

        private void OnValidate() {
            if (_raycasters != null) {
                _raycasters = _raycasters
                    .Where(r => r != null)
                    .OrderBy(r => r.GetComponent<Canvas>().sortingOrder)
                    .ToList();
            }
        }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 指定したスクリーン座標上にある最前面の<see cref="I_BE2_Drag"/>を取得する
        /// </summary>
        public I_BPG_Draggable GetDragAtPosition(Vector2 screenPosition) {
            // eventデータ
            var pointerEventData = new PointerEventData(EventSystem.current) {
                position = GetCanvasPosition(CanvasRenderMode, screenPosition)
            };

            // Raycasterリストを順番に処理し、最初にヒットした要素を評価
            foreach (var raycaster in _raycasters) {
                var results = new List<RaycastResult>();
                raycaster.Raycast(pointerEventData, results);

                foreach (var result in results) {
                    // I_BE2_Dragより前に他の要素があればnullを返す
                    var drag = result.gameObject.GetComponentInParent<I_BE2_Drag>();
                    if (drag != null) {
                        return drag;
                    } else {
                        // 他の要素がある場合はnullを返す
                        return null;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public I_BPG_Spot GetSpotAtPosition(Vector3 screenPosition) {
            // eventデータ
            var pointerEventData = new PointerEventData(EventSystem.current) {
                position = GetCanvasPosition(CanvasRenderMode, screenPosition)
            };

            // Spotコンポーネントの取得
            var spot = _raycasters
                .SelectMany(raycaster => {
                    var results = new List<RaycastResult>();
                    raycaster.Raycast(pointerEventData, results);
                    return results;
                })
                .Where(result => result.gameObject.activeSelf)
                .Select(result => result.gameObject.GetComponent<I_BPG_Spot>())
                .FirstOrDefault(x => x != null);

            return spot;
        }


        /// <summary>
        /// 指定された距離内で、最も近い指定された型のSpotを探します。
        /// </summary>
        public I_BPG_Spot FindClosestSpotOfType<TSPot>(I_BPG_Draggable drag, float maxDistance)
            where TSPot : I_BPG_Spot {
            return FindClosestSpot(drag, maxDistance, spot => spot is TSPot);
        }


        /// <summary>
        /// 指定された距離内で、最も近いSpotを探します。
        /// </summary>
        public I_BPG_Spot FindClosestSpotForBlock(I_BPG_Spot drag, float maxDistance) {
            return FindClosestSpot(drag, maxDistance, spot =>
                // Body領域 (ブロック内部)、
                (spot is BPG_SpotBlockBody ||
                // または対象ブロックが親を持っている
                (spot is BPG_SpotOuterArea && spot.Block?.ParentSection != null))
            );
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private Vector3 GetCanvasPosition(RenderMode mode, Vector3 screenPosition) {
            return mode switch {
                RenderMode.ScreenSpaceOverlay => screenPosition,
                RenderMode.ScreenSpaceCamera => Camera.main.WorldToScreenPoint(BE2_Pointer.Instance.Position),
                RenderMode.WorldSpace => Camera.main.WorldToScreenPoint(BE2_Pointer.Instance.Position),
                _ => throw new System.NotImplementedException()
            };
        }


        /// <summary>
        /// Spotを探す共通メソッド.
        /// </summary>
        private I_BPG_Spot FindClosestSpot(I_BPG_Draggable drag, float maxDistance, System.Func<I_BPG_Spot, bool> condition) {
            I_BPG_Spot foundSpot = null;
            float minDistance = Mathf.Infinity;

            // 全探索
            var targetSpots = BPG_Spot.ActiveSpots.Where(s => condition(s) && s.RectTransform.gameObject.activeSelf);
            foreach (var spot in targetSpots) {
                var d = spot.RectTransform.GetComponentInParent<I_BPG_Draggable>();
                if (d is null) {
                    continue;
                }

                var programmingEnv = d.RectTransform.GetComponentInParent<I_BPG_ProgrammingEnv>();

                if (d != drag && programmingEnv?.Visible == true) {
                    // 距離判定
                    float distance = Vector2.Distance(drag.RayPoint, spot.DropPosition);
                    if (distance < minDistance && distance <= maxDistance) {
                        foundSpot = spot;
                        minDistance = distance;
                    }
                }
            }

            return foundSpot;
        }

        */
    }

}