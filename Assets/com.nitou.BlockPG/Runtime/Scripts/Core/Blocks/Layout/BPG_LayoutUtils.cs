using UnityEngine;

namespace nitou.BlockPG.Blocks {

    /// <summary>
    /// ブロック階層の配置計算．
    /// </summary>
    /// <remarks>
    /// [NOTE] ブロックのサイズはライブラリ側が決めており、LayoutGroup は縦積みの位置決めしか
    ///        していない（全て childControl = false）．そのぶんの再構築コストを避けるため、
    ///        単純な積み上げは自前で行う．
    ///
    ///        ここで行う配置は VerticalLayoutGroup（padding 0 / spacing 0 / 左寄せ）と
    ///        同じ結果になるよう実測から導いている．
    ///          anchorMin = anchorMax = (0, 1)
    ///          x = 幅   * pivot.x
    ///          y = -積み上げ位置 - 高さ * (1 - pivot.y)
    /// </remarks>
    internal static class BPG_LayoutUtils {

        private static readonly Vector2 TopLeft = new(0f, 1f);

        /// <summary>
        /// 直下の子を上から順に縦へ積む．
        /// </summary>
        /// <remarks>
        /// サイズは変更しないため、呼び出し前に各子のサイズが確定している必要がある．
        /// </remarks>
        internal static void StackChildrenVertically(Transform parent) {
            if (parent == null)
                return;

            float cursor = 0f;
            foreach (Transform child in parent) {
                // ※LayoutGroup と同様に非アクティブな子は積み上げ対象から外す
                if (!child.gameObject.activeSelf)
                    continue;

                if (child is not RectTransform rect)
                    continue;

                var size = rect.sizeDelta;
                var pivot = rect.pivot;

                rect.anchorMin = TopLeft;
                rect.anchorMax = TopLeft;
                rect.anchoredPosition = new Vector2(
                    size.x * pivot.x,
                    -cursor - size.y * (1f - pivot.y));

                cursor += size.y;
            }
        }
    }
}
