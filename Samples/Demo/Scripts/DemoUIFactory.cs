using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace nitou.BlockPG.Demo {

    /// <summary>
    /// デモ用UIの生成ヘルパー．
    /// [NOTE] 角丸スプライトはアセットを持ち込まずに済むよう手続き的に生成する．
    ///        テーマごとに角丸・枠線の太さが変わるため、パラメータ単位でキャッシュする．
    /// </summary>
    public static class DemoUIFactory {

        /// <summary>
        /// 同梱フォントの Resources 上のパス．
        /// </summary>
        private const string JapaneseFontPath = "Fonts/MPLUS1p-Regular";

        private static readonly Dictionary<int, Sprite> _spriteCache = new();
        private static Font _font;


        /// ----------------------------------------------------------------------------
        // Sprite

        /// <summary>
        /// 角丸（＋任意で枠線）の9スライススプライトを取得する．
        /// </summary>
        public static Sprite GetRoundedSprite(int cornerRadius, int outlineWidth = 0) {
            cornerRadius = Mathf.Clamp(cornerRadius, 0, 48);
            outlineWidth = Mathf.Clamp(outlineWidth, 0, 8);

            int key = cornerRadius * 100 + outlineWidth;
            if (_spriteCache.TryGetValue(key, out var cached) && cached != null) {
                return cached;
            }

            // 中央を1pxだけ残し、四隅を9スライスの固定領域にする
            int edge = Mathf.Max(cornerRadius, outlineWidth) + 1;
            int size = edge * 2 + 1;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false) {
                name = $"Rounded_{cornerRadius}_{outlineWidth}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float coverage = RoundedRectCoverage(x, y, size, cornerRadius);

                    byte alpha = (byte)Mathf.RoundToInt(coverage * 255f);
                    Color32 color = new Color32(255, 255, 255, alpha);

                    // 枠線は白のまま、内側を透過させて「縁だけ描く」形にはせず、
                    // 別Imageで重ねる方式を取るため、ここでは塗りつぶしのみ生成する
                    pixels[y * size + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false);

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 100f,
                extrude: 0,
                meshType: SpriteMeshType.FullRect,
                border: new Vector4(edge, edge, edge, edge));
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;

            _spriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// 角丸矩形における指定ピクセルの被覆率．（※4x4のスーパーサンプリング）
        /// </summary>
        private static float RoundedRectCoverage(int px, int py, int size, int radius) {
            if (radius <= 0)
                return 1f;

            const int Samples = 4;
            int inside = 0;

            for (int sy = 0; sy < Samples; sy++) {
                for (int sx = 0; sx < Samples; sx++) {
                    float x = px + (sx + 0.5f) / Samples;
                    float y = py + (sy + 0.5f) / Samples;

                    // 最も近い角の中心からの距離で判定する
                    float cx = Mathf.Clamp(x, radius, size - radius);
                    float cy = Mathf.Clamp(y, radius, size - radius);

                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy <= radius * radius) {
                        inside++;
                    }
                }
            }
            return inside / (float)(Samples * Samples);
        }


        /// ----------------------------------------------------------------------------
        // Font

        /// <summary>
        /// 日本語を含むテキストを描画できるフォントを取得する．
        /// </summary>
        // [NOTE] 組み込みフォント(Liberation Sans)には日本語のグリフが無い．
        //        エディタでは Unity が OS 導入フォントへフォールバックするため気付けないが、
        //        WebGL にはフォールバック先が無く、日本語だけが無言で描画されなくなる．
        //        （実際に公開デモでコンテキストメニューの項目が消えていた）
        //        そのため同梱フォントを優先して読み込む．
        public static Font GetFont() {
            if (_font != null)
                return _font;

            _font = Resources.Load<Font>(JapaneseFontPath);
            if (_font != null)
                return _font;

            Debug.LogWarning($"Bundled font is not found. Japanese text will not be rendered " +
                $"on platforms without system fonts. (path: {JapaneseFontPath})");

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) {
                // ※古いバージョンではフォント名が異なる
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return _font;
        }


        /// ----------------------------------------------------------------------------
        // GameObject

        /// <summary>
        /// 画面全体を覆う Screen Space - Overlay の Canvas を作る．
        /// </summary>
        // [NOTE] デモシーンに置かれた Canvas と同じ基準にしてある．
        //        揃えないと、重ねたときに座標とスケールが食い違う．
        public static Canvas CreateOverlayCanvas(string name, int sortingOrder = 0) {
            var obj = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = obj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        /// <summary>
        /// RectTransform を持つ子オブジェクトを作る．
        /// </summary>
        public static RectTransform CreateRect(string name, Transform parent) {
            var obj = new GameObject(name, typeof(RectTransform));
            var rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            rect.localScale = Vector3.one;
            rect.anchoredPosition3D = Vector3.zero;
            return rect;
        }

        /// <summary>
        /// 画像つきの子オブジェクトを作る．
        /// </summary>
        public static Image CreateImage(string name, Transform parent, bool raycastTarget = true) {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>
        /// テキストつきの子オブジェクトを作る．
        /// </summary>
        public static Text CreateText(string name, Transform parent, string content, int fontSize,
            FontStyle style = FontStyle.Normal, TextAnchor anchor = TextAnchor.MiddleCenter) {

            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = GetFont();
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// ラベルつきボタンを作る．
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string label, int fontSize = 22) {
            var image = CreateImage(name, parent);
            image.type = Image.Type.Sliced;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.fadeDuration = 0.08f;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            button.colors = colors;

            var text = CreateText("Label", image.transform, label, fontSize, FontStyle.Bold);
            Stretch(text.rectTransform);

            return button;
        }


        /// ----------------------------------------------------------------------------
        // Layout

        /// <summary>
        /// 親いっぱいに広げる．
        /// </summary>
        public static void Stretch(RectTransform rect, float padding = 0f) {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>
        /// アンカーとサイズをまとめて設定する．
        /// </summary>
        public static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax) {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
