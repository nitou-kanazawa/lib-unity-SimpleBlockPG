using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace nitou.BlockPG.Demo {

    /// <summary>
    /// デモシーンに Hub へ戻るボタンを差し込む．
    /// </summary>
    // [NOTE] 各デモのUI構築コードには手を入れず、シーン読み込み後に上から重ねる．
    //        デモ側のレイアウトが変わっても影響を受けないようにするため．
    //        配置はテーマバー右側の空き領域に合わせてある．
    public static class DemoNavigation {

        private const int SortingOrder = 1000;
        private const float ButtonWidth = 188f;
        private const float ButtonHeight = 60f;
        private const float MarginRight = 32f;
        private const float MarginBottom = 30f;

        private static bool _subscribed;


        /// ----------------------------------------------------------------------------
        // Entry Point

        // [NOTE] ドメインリロードを無効にしていると静的変数が実行をまたいで残るため、明示的に戻す．
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() {
            _subscribed = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize() {
            if (_subscribed)
                return;
            _subscribed = true;

            SceneManager.sceneLoaded += OnSceneLoaded;

            // ※起動時に開かれたシーンには sceneLoaded が飛ばないため、ここで一度処理する
            TryCreateBackButton(SceneManager.GetActiveScene());
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (mode != LoadSceneMode.Single)
                return;

            TryCreateBackButton(scene);
        }

        private static void TryCreateBackButton(Scene scene) {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            // ※Hub自身には要らない
            if (scene.name == DemoSceneCatalog.HubSceneName)
                return;

            // ※Samples としてインポートされた環境ではHubがビルド設定に無い．
            //   その場合は戻り先が存在しないため、ボタンごと出さない．
            if (!DemoSceneCatalog.IsInBuild(DemoSceneCatalog.HubSceneName))
                return;

            CreateBackButton(scene, DemoTheme.Scratch());
        }

        private static void CreateBackButton(Scene scene, DemoTheme theme) {
            var canvas = DemoUIFactory.CreateOverlayCanvas("DemoNavigation", SortingOrder);

            // ※シーンを切り替えたときに一緒に破棄させる
            SceneManager.MoveGameObjectToScene(canvas.gameObject, scene);

            var button = DemoUIFactory.CreateButton("Button (Back to Hub)", canvas.transform, "Back to Hub", 20);
            button.image.sprite = DemoUIFactory.GetRoundedSprite(theme.ButtonCornerRadius);
            button.image.color = theme.ButtonColor;

            var label = button.GetComponentInChildren<Text>();
            if (label != null) {
                label.color = theme.ButtonTextColor;
            }

            DemoUIFactory.SetAnchored(button.image.rectTransform,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-(MarginRight + ButtonWidth), MarginBottom),
                new Vector2(-MarginRight, MarginBottom + ButtonHeight));

            button.onClick.AddListener(() => SceneManager.LoadScene(DemoSceneCatalog.HubSceneName));
        }
    }
}
