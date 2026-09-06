using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace nitou.BlockPG.Demo {

    /// <summary>
    /// デモ用のコンテキストメニュー．
    /// </summary>
    /// <remarks>
    /// [NOTE] 右クリックと長押しは <c>OnSecondaryAction</c> に束ねられているため、
    ///        呼び出し側はプラットフォームを意識しない．
    /// </remarks>
    public sealed class DemoContextMenu {

        private const float ItemHeight = 52f;
        private const float MenuWidth = 200f;
        private const float Padding = 6f;

        private readonly RectTransform _root;
        private readonly Image _blocker;
        private readonly Image _panel;
        private readonly List<Image> _itemImages = new();
        private readonly List<Text> _itemLabels = new();

        // ※項目は開くたびに作り直すため、最後に適用したテーマを覚えておく
        private DemoTheme _theme;

        /// <summary>表示中かどうか．</summary>
        public bool IsOpen => _blocker.gameObject.activeSelf;


        /// ----------------------------------------------------------------------------
        // Public Method

        public DemoContextMenu(RectTransform canvasRoot) {
            _root = canvasRoot;

            // ※メニュー外のクリックを拾って閉じるための透明な板
            _blocker = DemoUIFactory.CreateImage("ContextMenuBlocker", canvasRoot);
            DemoUIFactory.Stretch(_blocker.rectTransform);
            _blocker.color = new Color(0f, 0f, 0f, 0.001f);
            _blocker.gameObject.AddComponent<Button>().onClick.AddListener(Close);

            _panel = DemoUIFactory.CreateImage("ContextMenu", _blocker.transform);
            _panel.type = Image.Type.Sliced;
            _panel.rectTransform.pivot = new Vector2(0f, 1f);
            _panel.rectTransform.anchorMin = new Vector2(0f, 0f);
            _panel.rectTransform.anchorMax = new Vector2(0f, 0f);

            _blocker.gameObject.SetActive(false);
        }

        /// <summary>
        /// 指定のスクリーン座標へメニューを開く．
        /// </summary>
        public void Open(Vector2 screenPosition, IReadOnlyList<(string label, Action action)> items) {
            if (items == null || items.Count == 0)
                return;

            BuildItems(items);

            float height = items.Count * ItemHeight + Padding * 2f;
            _panel.rectTransform.sizeDelta = new Vector2(MenuWidth, height);

            _blocker.gameObject.SetActive(true);
            Place(screenPosition, height);
        }

        /// <summary>
        /// メニューを閉じる．
        /// </summary>
        public void Close() {
            _blocker.gameObject.SetActive(false);
        }

        /// <summary>
        /// 見た目を適用する．
        /// </summary>
        public void ApplyTheme(DemoTheme theme) {
            if (theme == null)
                return;

            _theme = theme;
            _panel.sprite = DemoUIFactory.GetRoundedSprite(theme.PanelCornerRadius);
            _panel.color = theme.PanelColor;

            var itemSprite = DemoUIFactory.GetRoundedSprite(theme.ButtonCornerRadius);
            foreach (var image in _itemImages) {
                image.sprite = itemSprite;
                image.type = Image.Type.Sliced;
                image.color = theme.ButtonColor;
            }
            foreach (var label in _itemLabels) {
                label.color = theme.ButtonTextColor;
            }
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private void BuildItems(IReadOnlyList<(string label, Action action)> items) {
            // ※項目数は都度変わるため作り直す（デモ用途では十分）
            foreach (var image in _itemImages) {
                UnityEngine.Object.Destroy(image.gameObject);
            }
            _itemImages.Clear();
            _itemLabels.Clear();

            for (int i = 0; i < items.Count; i++) {
                var item = items[i];
                var button = DemoUIFactory.CreateButton($"Item ({item.label})", _panel.transform, item.label, 20);

                float top = -(Padding + i * ItemHeight);
                DemoUIFactory.SetAnchored(button.image.rectTransform,
                    new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(Padding, top - ItemHeight + 4f), new Vector2(-Padding, top));

                var action = item.action;
                button.onClick.AddListener(() => {
                    Close();
                    action?.Invoke();
                });

                _itemImages.Add(button.image);
                _itemLabels.Add(button.GetComponentInChildren<Text>());
            }

            // ※作り直した項目へ改めて色を当てる
            // （Text の既定色は白のため、当てないと白パネルに白文字で見えなくなる）
            ApplyTheme(_theme);
        }

        /// <summary>
        /// 画面外へはみ出さない位置へ配置する．
        /// </summary>
        private void Place(Vector2 screenPosition, float height) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root, screenPosition, null, out var local);

            var rootRect = _root.rect;

            // ※右端・下端で反転させる
            float x = local.x;
            if (x + MenuWidth > rootRect.xMax) {
                x -= MenuWidth;
            }
            float y = local.y;
            if (y - height < rootRect.yMin) {
                y += height;
            }

            _panel.rectTransform.anchoredPosition =
                new Vector2(x - rootRect.xMin, y - rootRect.yMin);
        }
    }
}
