using UnityEngine;
using UnityEngine.Pool;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// 積み上げの設定．
    /// </summary>
    /// <remarks>
    /// 余白と間隔は uGUI の LayoutGroup と同じ意味を持つ．
    /// <see cref="Alignment"/> は開始辺からの割合で、
    /// x は 0=左 / 1=右、y は 0=上 / 1=下 とする．
    /// </remarks>
    internal readonly struct BPG_StackSettings {

        public readonly bool Vertical;
        public readonly float PaddingLeft;
        public readonly float PaddingRight;
        public readonly float PaddingTop;
        public readonly float PaddingBottom;
        public readonly float Spacing;
        public readonly Vector2 Alignment;

        public BPG_StackSettings(bool vertical, float spacing = 0f,
            float paddingLeft = 0f, float paddingRight = 0f,
            float paddingTop = 0f, float paddingBottom = 0f,
            Vector2 alignment = default) {

            Vertical = vertical;
            Spacing = spacing;
            PaddingLeft = paddingLeft;
            PaddingRight = paddingRight;
            PaddingTop = paddingTop;
            PaddingBottom = paddingBottom;
            Alignment = alignment;
        }

        /// <summary>余白も間隔も無い、左上詰めの縦積み．</summary>
        public static BPG_StackSettings SimpleVertical => new(vertical: true);
    }


    /// <summary>
    /// ブロック階層の配置計算．
    /// </summary>
    /// <remarks>
    /// [NOTE] ブロックのサイズはライブラリ側が決めるため、uGUI の LayoutGroup は使わない．
    ///        位置決めもここで完結させることで、確保した領域と描画内容が食い違わないようにする．
    ///
    ///        配置ルールは LayoutGroup が実際に設定していた値を実行時に採取して導いている．
    ///          anchorMin = anchorMax = (0, 1)
    ///          x = 開始辺からの距離 + 幅   * pivot.x
    ///          y = -開始辺からの距離 - 高さ * (1 - pivot.y)
    ///
    ///        積み上げから外したい子には <see cref="BPG_LayoutIgnore"/> を付ける．
    /// </remarks>
    internal static class BPG_LayoutUtils {

        private static readonly Vector2 TopLeft = new(0f, 1f);


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 直下の子を上から順に縦へ積む．（※余白・間隔なし）
        /// </summary>
        internal static void StackChildrenVertically(Transform parent) {
            if (parent is RectTransform rect) {
                StackChildren(rect, BPG_StackSettings.SimpleVertical);
            }
        }

        /// <summary>
        /// 直下の子を指定方向へ積む．
        /// </summary>
        /// <remarks>
        /// サイズは変更しないため、呼び出し前に親と各子のサイズが確定している必要がある．
        /// </remarks>
        internal static void StackChildren(RectTransform parent, in BPG_StackSettings settings) {
            if (parent == null)
                return;

            var targets = ListPool<RectTransform>.Get();
            foreach (Transform child in parent) {
                if (child is RectTransform rect && !IsIgnored(rect)) {
                    targets.Add(rect);
                }
            }

            if (targets.Count > 0) {
                Arrange(parent, targets, settings);
            }
            ListPool<RectTransform>.Release(targets);
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private static void Arrange(RectTransform parent,
            System.Collections.Generic.List<RectTransform> targets, in BPG_StackSettings settings) {

            bool vertical = settings.Vertical;
            var parentSize = parent.rect.size;

            // 積み上げ方向と交差方向を、それぞれ開始辺からの距離として扱う
            float mainPadStart = vertical ? settings.PaddingTop : settings.PaddingLeft;
            float mainPadEnd = vertical ? settings.PaddingBottom : settings.PaddingRight;
            float crossPadStart = vertical ? settings.PaddingLeft : settings.PaddingTop;
            float crossPadEnd = vertical ? settings.PaddingRight : settings.PaddingBottom;

            float mainAlignment = vertical ? settings.Alignment.y : settings.Alignment.x;
            float crossAlignment = vertical ? settings.Alignment.x : settings.Alignment.y;

            // 積み上げ方向の合計
            float content = settings.Spacing * (targets.Count - 1);
            foreach (var target in targets) {
                content += vertical ? target.sizeDelta.y : target.sizeDelta.x;
            }

            float mainAvailable = (vertical ? parentSize.y : parentSize.x) - mainPadStart - mainPadEnd;
            float cursor = mainPadStart + (mainAvailable - content) * mainAlignment;

            float crossAvailable = (vertical ? parentSize.x : parentSize.y) - crossPadStart - crossPadEnd;

            foreach (var target in targets) {
                var size = target.sizeDelta;
                var pivot = target.pivot;

                float mainSize = vertical ? size.y : size.x;
                float crossSize = vertical ? size.x : size.y;
                float crossOffset = crossPadStart + (crossAvailable - crossSize) * crossAlignment;

                float fromLeft = vertical ? crossOffset : cursor;
                float fromTop = vertical ? cursor : crossOffset;

                target.anchorMin = TopLeft;
                target.anchorMax = TopLeft;
                target.anchoredPosition = new Vector2(
                    fromLeft + size.x * pivot.x,
                    -fromTop - size.y * (1f - pivot.y));

                cursor += mainSize + settings.Spacing;
            }
        }

        /// <summary>
        /// 積み上げ対象から外すべき子かどうか判定する．
        /// </summary>
        /// <remarks>
        /// [NOTE] 非アクティブな子と、<see cref="I_BPG_LayoutIgnore"/> で除外指定された子を対象外とする．
        ///        無効化されたコンポーネントの指定は尊重しない（Unity の慣例に合わせる）．
        /// </remarks>
        private static bool IsIgnored(RectTransform rect) {
            if (!rect.gameObject.activeSelf)
                return true;

            var ignorers = ListPool<Component>.Get();
            rect.GetComponents(typeof(I_BPG_LayoutIgnore), ignorers);

            bool ignored = false;
            for (int i = 0; i < ignorers.Count; i++) {
                if (ignorers[i] is Behaviour { isActiveAndEnabled: false })
                    continue;

                if (ignorers[i] is I_BPG_LayoutIgnore { IgnoreLayout: true }) {
                    ignored = true;
                    break;
                }
            }

            ListPool<Component>.Release(ignorers);
            return ignored;
        }
    }
}
