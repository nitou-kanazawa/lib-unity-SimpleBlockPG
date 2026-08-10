using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.Blocks.Section;
using nitou.BlockPG.Enviorment;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

// [NOTE] BPG_BlockUtils は nitou.BlockPG（生成）と nitou.BlockPG.Blocks（破棄）の
//        2つの名前空間に同名で存在するため、明示的に解決する．
using BlockUtils = nitou.BlockPG.BPG_BlockUtils;

namespace nitou.BlockPG.Demo {

    /// <summary>
    /// デモで使用するブロックの一覧．
    /// </summary>
    public static class DemoBlockCatalog {

        public const string Entry = "Block [Entry]";
        public const string Normal = "Block [Normal]";
        public const string Scope = "Block [Scope]";
        public const string MultiScope = "Block [MultiScope]";

        /// <summary>パレットに並べるブロックと表示名．</summary>
        public static readonly (string prefabName, string label)[] Items = {
            (Entry,      "Entry"),
            (Normal,     "Normal"),
            (Scope,      "Scope"),
            (MultiScope, "MultiScope"),
        };
    }


    /// <summary>
    /// SimpleBlockPG のデモ．
    /// ブロックの組み立て、保存・読み込み、テーマ切り替えを一通り試せる．
    /// [NOTE] UIはシーンに置かず実行時に構築する．テーマごとの見た目差分をコードで一望できるようにするため．
    /// </summary>
    public sealed class BlockPGDemo : MonoBehaviour {

        [Header("Scene references")]
        [SerializeField] private BPG_ProgrammingEnv _workspace;
        [SerializeField] private RectTransform _canvasRoot;
        [SerializeField] private RectTransform _draggingLayer;

        [Header("Layout")]
        [SerializeField] private float _topBarHeight = 84f;
        [SerializeField] private float _paletteWidth = 280f;
        [SerializeField] private float _themeBarHeight = 104f;

        // テーマ
        private DemoTheme[] _themes;
        private int _themeIndex = 0;

        // 実行時に構築するUI
        private Image _background;
        private Image _topBar;
        private Image _palette;
        private Image _themeBar;
        private Image _workspaceImage;
        private Text _statusText;
        private Text _titleText;
        private Text _hintText;
        private readonly List<Text> _headings = new();
        private readonly List<Image> _buttonImages = new();
        private readonly List<Text> _buttonLabels = new();
        private readonly List<Image> _themeButtons = new();
        private readonly List<Text> _themeButtonLabels = new();

        // 生成位置のずらし幅
        private int _spawnCount = 0;

        private string SavePath => BPG_BlockStorage.GetDefaultPath("demo-workspace");


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        private void Start() {
            if (_workspace == null || _canvasRoot == null) {
                Debug.LogError("Demo references are not assigned.", this);
                enabled = false;
                return;
            }

            _themes = DemoTheme.CreateAll();

            BuildUI();
            ApplyTheme(_themes[_themeIndex]);

            SetStatus("パレットのボタンでブロックを追加できます。");
        }


        /// ----------------------------------------------------------------------------
        // UI 構築

        private void BuildUI() {
            // 背景
            _background = DemoUIFactory.CreateImage("Background", _canvasRoot, raycastTarget: false);
            DemoUIFactory.Stretch(_background.rectTransform);

            // ワークスペース（＝ブロックの配置先）
            // [NOTE] ここだけを Spot の当たり範囲にすることで、外へドラッグしたブロックが破棄される．
            _workspaceImage = _workspace.GetComponent<Image>();
            if (_workspaceImage == null) {
                _workspaceImage = _workspace.gameObject.AddComponent<Image>();
            }
            _workspaceImage.raycastTarget = true;
            DemoUIFactory.SetAnchored(_workspace.RectTransform,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(_paletteWidth, _themeBarHeight),
                offsetMax: new Vector2(0f, -_topBarHeight));

            BuildTopBar();
            BuildPalette();
            BuildThemeBar();

            // 描画順を整える
            _background.rectTransform.SetAsFirstSibling();
            _workspace.RectTransform.SetSiblingIndex(1);
            if (_draggingLayer != null) {
                _draggingLayer.SetAsLastSibling();
            }
        }

        private void BuildTopBar() {
            _topBar = DemoUIFactory.CreateImage("TopBar", _canvasRoot);
            _topBar.type = Image.Type.Sliced;
            DemoUIFactory.SetAnchored(_topBar.rectTransform,
                anchorMin: new Vector2(0f, 1f), anchorMax: Vector2.one,
                offsetMin: new Vector2(8f, -_topBarHeight + 4f), offsetMax: new Vector2(-8f, -6f));

            _titleText = DemoUIFactory.CreateText("Title", _topBar.transform,
                "SimpleBlockPG  Demo", 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            DemoUIFactory.SetAnchored(_titleText.rectTransform,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 0f), new Vector2(360f, 0f));

            // 操作ボタン
            var actions = new (string label, System.Action action)[] {
                ("Save",  Save),
                ("Load",  Load),
                ("Clear", Clear),
            };

            const float buttonWidth = 132f;
            const float spacing = 12f;
            for (int i = 0; i < actions.Length; i++) {
                var button = DemoUIFactory.CreateButton($"Button ({actions[i].label})", _topBar.transform, actions[i].label);
                float right = -(20f + i * (buttonWidth + spacing));

                DemoUIFactory.SetAnchored(button.image.rectTransform,
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(right - buttonWidth, -24f), new Vector2(right, 24f));

                var action = actions[i].action;
                button.onClick.AddListener(() => action());

                _buttonImages.Add(button.image);
                _buttonLabels.Add(button.GetComponentInChildren<Text>());
            }

            _statusText = DemoUIFactory.CreateText("Status", _topBar.transform, "", 17,
                FontStyle.Normal, TextAnchor.MiddleLeft);
            DemoUIFactory.SetAnchored(_statusText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(392f, 0f), new Vector2(-460f, 0f));
        }

        private void BuildPalette() {
            _palette = DemoUIFactory.CreateImage("Palette", _canvasRoot);
            _palette.type = Image.Type.Sliced;
            DemoUIFactory.SetAnchored(_palette.rectTransform,
                anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(0f, 1f),
                offsetMin: new Vector2(8f, _themeBarHeight + 2f),
                offsetMax: new Vector2(_paletteWidth - 8f, -_topBarHeight - 2f));

            var heading = DemoUIFactory.CreateText("Heading", _palette.transform, "BLOCKS", 16,
                FontStyle.Bold, TextAnchor.MiddleLeft);
            DemoUIFactory.SetAnchored(heading.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -52f), new Vector2(-16f, -18f));
            _headings.Add(heading);

            const float buttonHeight = 62f;
            const float spacing = 14f;
            for (int i = 0; i < DemoBlockCatalog.Items.Length; i++) {
                var item = DemoBlockCatalog.Items[i];
                var button = DemoUIFactory.CreateButton($"Button ({item.label})", _palette.transform, item.label, 21);

                float top = -66f - i * (buttonHeight + spacing);
                DemoUIFactory.SetAnchored(button.image.rectTransform,
                    new Vector2(0f, 1f), new Vector2(1f, 1f),
                    new Vector2(18f, top - buttonHeight), new Vector2(-18f, top));

                var prefabName = item.prefabName;
                button.onClick.AddListener(() => Spawn(prefabName));

                _buttonImages.Add(button.image);
                _buttonLabels.Add(button.GetComponentInChildren<Text>());
            }

            _hintText = DemoUIFactory.CreateText("Hint", _palette.transform,
                "ブロックをドラッグして\n重ねると連結します。\n\nワークスペースの外へ\n出すと削除されます。",
                16, FontStyle.Normal, TextAnchor.LowerLeft);
            _hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            DemoUIFactory.SetAnchored(_hintText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(22f, 20f), new Vector2(-16f, 160f));
        }

        private void BuildThemeBar() {
            _themeBar = DemoUIFactory.CreateImage("ThemeBar", _canvasRoot);
            _themeBar.type = Image.Type.Sliced;
            DemoUIFactory.SetAnchored(_themeBar.rectTransform,
                anchorMin: Vector2.zero, anchorMax: new Vector2(1f, 0f),
                offsetMin: new Vector2(8f, 8f), offsetMax: new Vector2(-8f, _themeBarHeight - 8f));

            var heading = DemoUIFactory.CreateText("Heading", _themeBar.transform, "THEME", 16,
                FontStyle.Bold, TextAnchor.MiddleLeft);
            DemoUIFactory.SetAnchored(heading.rectTransform,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 0f), new Vector2(120f, 0f));
            _headings.Add(heading);

            const float buttonWidth = 176f;
            const float spacing = 14f;
            for (int i = 0; i < _themes.Length; i++) {
                var theme = _themes[i];
                var button = DemoUIFactory.CreateButton($"Theme ({theme.Name})", _themeBar.transform, theme.Name, 20);

                float left = 128f + i * (buttonWidth + spacing);
                DemoUIFactory.SetAnchored(button.image.rectTransform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(left, -30f), new Vector2(left + buttonWidth, 30f));

                int index = i;
                button.onClick.AddListener(() => SelectTheme(index));

                _themeButtons.Add(button.image);
                _themeButtonLabels.Add(button.GetComponentInChildren<Text>());
            }
        }


        /// ----------------------------------------------------------------------------
        // 操作

        /// <summary>
        /// ブロックを生成してワークスペースへ置く．
        /// </summary>
        public void Spawn(string prefabName) {
            var prefab = BlockUtils.LoadBlockPrefab(prefabName);
            if (prefab == null) {
                SetStatus($"プレハブが見つかりません: {prefabName}");
                return;
            }

            var block = BlockUtils.CreateBlock(prefab, _workspace);

            // 少しずつずらして重ならないようにする
            int step = _spawnCount++ % 8;
            block.RectTransform.anchoredPosition = new Vector2(80f + step * 26f, -80f - step * 30f);

            ApplyThemeToBlocks(CurrentTheme);
            SetStatus($"{prefabName} を追加しました。");
        }

        /// <summary>
        /// ワークスペースの内容を保存する．
        /// </summary>
        public void Save() {
            var roots = GetRootBlocks();
            BPG_BlockStorage.Save(SavePath, roots);
            SetStatus($"{roots.Count} 個のブロックを保存しました。 ({SavePath})");
        }

        /// <summary>
        /// 保存済みの内容を読み込む．
        /// </summary>
        public void Load() {
            if (!BPG_BlockStorage.Exists(SavePath)) {
                SetStatus("保存データがありません。先に Save を実行してください。");
                return;
            }

            ClearBlocks();

            // [NOTE] 復元は同期的に完了するため、この行の直後には階層が組み上がっている．
            var restored = BPG_BlockStorage.Load(SavePath, _workspace);

            ApplyThemeToBlocks(CurrentTheme);
            SetStatus($"{restored.Count} 個のブロックを読み込みました。");
        }

        /// <summary>
        /// ワークスペースを空にする．
        /// </summary>
        public void Clear() {
            int count = GetRootBlocks().Count;
            ClearBlocks();
            SetStatus(count > 0 ? $"{count} 個のブロックを削除しました。" : "ワークスペースは空です。");
        }

        /// <summary>
        /// テーマを切り替える．
        /// </summary>
        public void SelectTheme(int index) {
            if (index < 0 || index >= _themes.Length)
                return;

            _themeIndex = index;
            ApplyTheme(_themes[index]);
            SetStatus($"テーマを {_themes[index].Name} に切り替えました。");
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private DemoTheme CurrentTheme => _themes[_themeIndex];

        private IReadOnlyList<I_BPG_Block> GetRootBlocks() {
            var blocks = new List<I_BPG_Block>();
            foreach (Transform child in _workspace.RectTransform) {
                // [NOTE] Destroy はフレーム終端まで遅延するため、同じフレーム内では
                //        破棄予定のブロックも子として残っている．非アクティブ化を目印に除外する．
                if (child.gameObject.activeSelf && child.TryGetComponent<I_BPG_Block>(out var block)) {
                    blocks.Add(block);
                }
            }
            return blocks;
        }

        private void ClearBlocks() {
            foreach (var block in GetRootBlocks()) {
                Destroy(block.RectTransform.gameObject);
                // ※Destroy はフレーム終端まで遅延するため、直後の走査から外れるよう無効化する
                block.RectTransform.gameObject.SetActive(false);
            }
            _spawnCount = 0;
        }

        private void SetStatus(string message) {
            if (_statusText != null) {
                _statusText.text = message;
            }
        }


        /// ----------------------------------------------------------------------------
        // テーマ適用

        private void ApplyTheme(DemoTheme theme) {
            ApplyThemeToChrome(theme);
            ApplyThemeToBlocks(theme);
        }

        private void ApplyThemeToChrome(DemoTheme theme) {
            var panelSprite = DemoUIFactory.GetRoundedSprite(theme.PanelCornerRadius);
            var buttonSprite = DemoUIFactory.GetRoundedSprite(theme.ButtonCornerRadius);

            // 背景（上下のグラデーションは単色2枚では表現できないため中間色で近似する）
            _background.sprite = null;
            _background.color = Color.Lerp(theme.BackgroundTop, theme.BackgroundBottom, 0.5f);

            // ワークスペースは背景よりわずかに沈ませる
            _workspaceImage.sprite = panelSprite;
            _workspaceImage.type = Image.Type.Sliced;
            _workspaceImage.color = Color.Lerp(theme.BackgroundBottom, theme.PanelColor, 0.25f);

            foreach (var panel in new[] { _topBar, _palette, _themeBar }) {
                panel.sprite = panelSprite;
                panel.type = Image.Type.Sliced;
                panel.color = theme.PanelColor;
                ApplyOutline(panel.gameObject, theme);
            }

            _titleText.color = theme.TextColor;
            _statusText.color = theme.SubTextColor;
            _hintText.color = theme.SubTextColor;
            foreach (var heading in _headings) {
                heading.color = theme.AccentColor;
            }

            foreach (var image in _buttonImages) {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
                image.color = theme.ButtonColor;
                ApplyOutline(image.gameObject, theme);
            }
            foreach (var label in _buttonLabels) {
                label.color = theme.ButtonTextColor;
            }

            // テーマボタンは選択中のものを強調する
            for (int i = 0; i < _themeButtons.Count; i++) {
                bool selected = (i == _themeIndex);
                _themeButtons[i].sprite = buttonSprite;
                _themeButtons[i].type = Image.Type.Sliced;
                _themeButtons[i].color = selected ? theme.AccentColor : theme.ButtonColor;
                ApplyOutline(_themeButtons[i].gameObject, theme);

                _themeButtonLabels[i].color = selected ? theme.PanelColor : theme.ButtonTextColor;
            }
        }

        private static void ApplyOutline(GameObject target, DemoTheme theme) {
            var outline = target.GetComponent<Outline>();
            if (theme.PanelOutlineWidth <= 0) {
                if (outline != null) {
                    outline.enabled = false;
                }
                return;
            }

            if (outline == null) {
                outline = target.AddComponent<Outline>();
            }
            outline.enabled = true;
            outline.effectColor = theme.PanelOutlineColor;
            outline.effectDistance = new Vector2(theme.PanelOutlineWidth, theme.PanelOutlineWidth);
            outline.useGraphicAlpha = false;
        }

        /// <summary>
        /// 生成済みのブロックへテーマを反映する．
        /// </summary>
        private void ApplyThemeToBlocks(DemoTheme theme) {
            var itemSprite = DemoUIFactory.GetRoundedSprite(theme.ItemCornerRadius);

            foreach (var layout in _workspace.RectTransform.GetComponentsInChildren<BPG_BlockVerticalLayout>(true)) {
                // ※CreateBlock がプレハブ名をそのまま付けるため、名前で色を引ける
                layout.Color = theme.GetBlockColor(layout.name);
                layout.SetLayoutDirty();

                foreach (var item in layout.GetComponentsInChildren<BPG_BlockSectionHeader_Item>(true)) {
                    if (item.TryGetComponent<Image>(out var image)) {
                        image.sprite = itemSprite;
                        image.type = Image.Type.Sliced;
                        image.color = theme.ItemColor;
                    }
                }

                foreach (var shadow in layout.GetComponentsInChildren<Shadow>(true)) {
                    // ※Outline は Shadow の派生なので除外する
                    if (shadow is Outline)
                        continue;

                    shadow.enabled = theme.ShadowEnabled;
                    shadow.effectDistance = theme.ShadowDistance;
                    shadow.effectColor = theme.ShadowColor;
                }
            }
        }
    }
}
