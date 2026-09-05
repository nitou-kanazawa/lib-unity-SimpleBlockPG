using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace nitou.BlockPG.Demo {

    /// <summary>
    /// デモシーンの入口．一覧を並べ、選ばれたシーンへ遷移する．
    /// </summary>
    // [NOTE] UIはシーンに置かず実行時に構築する．他のデモシーンと同じ方針．
    //        シーンに置くのはこのコンポーネントとカメラ・EventSystem だけ．
    public sealed class DemoHub : MonoBehaviour {

        private const float PanelWidth = 860f;
        private const float CardHeight = 108f;
        private const float CardSpacing = 16f;
        private const float ListTop = -228f;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        private void Start() {
            Build(DemoTheme.Scratch());
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private void Build(DemoTheme theme) {
            var root = (RectTransform)DemoUIFactory.CreateOverlayCanvas("Canvas").transform;

            BuildBackground(root, theme);
            BuildHeader(root, theme);
            BuildList(root, theme);
        }

        private void BuildBackground(RectTransform root, DemoTheme theme) {
            var background = DemoUIFactory.CreateImage("Background", root, raycastTarget: false);
            background.color = theme.BackgroundTop;
            DemoUIFactory.Stretch(background.rectTransform);
        }

        private void BuildHeader(RectTransform root, DemoTheme theme) {
            var title = DemoUIFactory.CreateText("Title", root, "SimpleBlockPG  Demo", 46,
                FontStyle.Bold, TextAnchor.MiddleCenter);
            title.color = theme.TextColor;
            DemoUIFactory.SetAnchored(title.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-PanelWidth * 0.5f, -136f), new Vector2(PanelWidth * 0.5f, -72f));

            var subtitle = DemoUIFactory.CreateText("Subtitle", root, "試したいデモを選んでください", 22,
                FontStyle.Normal, TextAnchor.MiddleCenter);
            subtitle.color = theme.SubTextColor;
            DemoUIFactory.SetAnchored(subtitle.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-PanelWidth * 0.5f, -178f), new Vector2(PanelWidth * 0.5f, -140f));
        }

        private void BuildList(RectTransform root, DemoTheme theme) {
            float top = ListTop;

            foreach (var entry in DemoSceneCatalog.Scenes) {

                // ※押しても遷移できないため、ビルド設定に無いシーンは並べない
                if (!DemoSceneCatalog.IsInBuild(entry.SceneName)) {
                    Debug.LogWarning($"Demo scene is not in the build settings. (scene: {entry.SceneName})");
                    continue;
                }

                BuildCard(root, theme, entry, top);
                top -= CardHeight + CardSpacing;
            }
        }

        private void BuildCard(RectTransform root, DemoTheme theme, DemoSceneCatalog.Entry entry, float top) {
            var image = DemoUIFactory.CreateImage($"Card ({entry.SceneName})", root);
            image.type = Image.Type.Sliced;
            image.sprite = DemoUIFactory.GetRoundedSprite(theme.ButtonCornerRadius);
            image.color = theme.PanelColor;
            DemoUIFactory.SetAnchored(image.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-PanelWidth * 0.5f, top - CardHeight), new Vector2(PanelWidth * 0.5f, top));

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            // ※foreach の変数をそのまま捕捉しないよう控えを取る
            var sceneName = entry.SceneName;
            button.onClick.AddListener(() => SceneManager.LoadScene(sceneName));

            var title = DemoUIFactory.CreateText("Title", image.transform, entry.Title, 28,
                FontStyle.Bold, TextAnchor.LowerLeft);
            title.color = theme.TextColor;
            DemoUIFactory.SetAnchored(title.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f),
                new Vector2(28f, 0f), new Vector2(-28f, -16f));

            var description = DemoUIFactory.CreateText("Description", image.transform, entry.Description, 19,
                FontStyle.Normal, TextAnchor.UpperLeft);
            description.color = theme.SubTextColor;
            DemoUIFactory.SetAnchored(description.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f),
                new Vector2(28f, 16f), new Vector2(-28f, 0f));
        }
    }
}
