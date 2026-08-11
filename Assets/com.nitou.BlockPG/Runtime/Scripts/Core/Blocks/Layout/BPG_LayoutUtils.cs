using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;

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
        ///
        /// 積み上げから外したい子には <see cref="UnityEngine.UI.LayoutElement"/> を付けて
        /// ignoreLayout を有効にする．（※選択枠やバッジなどの装飾を重ねる用途を想定）
        /// </remarks>
        internal static void StackChildrenVertically(Transform parent) {
            if (parent == null)
                return;

            float cursor = 0f;
            foreach (Transform child in parent) {
                if (child is not RectTransform rect)
                    continue;
                if (IsIgnored(rect))
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

        /// <summary>
        /// 積み上げ対象から外すべき子かどうか判定する．
        /// </summary>
        /// <remarks>
        /// [NOTE] LayoutGroup と同じ条件に揃える．
        ///        非アクティブな子と、ignoreLayout が有効な子を対象外とする．
        /// </remarks>
        private static bool IsIgnored(RectTransform rect) {
            if (!rect.gameObject.activeSelf)
                return true;

            // [NOTE] LayoutGroup と同様、無効化されたコンポーネントの指定は尊重しない．
            var ignorers = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutIgnorer), ignorers);

            bool ignored = false;
            for (int i = 0; i < ignorers.Count; i++) {
                if (ignorers[i] is Behaviour { isActiveAndEnabled: false })
                    continue;

                if (ignorers[i] is ILayoutIgnorer { ignoreLayout: true }) {
                    ignored = true;
                    break;
                }
            }

            ListPool<Component>.Release(ignorers);
            return ignored;
        }
    }
}
