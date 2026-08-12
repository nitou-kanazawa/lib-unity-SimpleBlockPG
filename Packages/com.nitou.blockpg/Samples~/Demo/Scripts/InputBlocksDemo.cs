using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.Blocks.Section;
using nitou.BlockPG.Environments;
using nitou.BlockPG.Events;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

namespace nitou.BlockPG.Demo {

    /// <summary>
    /// 入力を持つブロックの一覧．
    /// </summary>
    /// <remarks>
    /// [NOTE] プレハブはこのデモ専用．ライブラリ同梱のブロックには入力要素を持たせていない．
    ///        既存デモ（06-Playground）の見た目を変えないため．
    /// </remarks>
    public static class InputBlockCatalog {

        public const string Say = "Block [Say]";
        public const string Wait = "Block [Wait]";
        public const string Move = "Block [Move]";
        public const string Repeat = "Block [Scope]";
        public const string Entry = "Block [Entry]";

        public static readonly (string prefabName, string label)[] Items = {
            (Entry,  "Entry"),
            (Say,    "Say  [文字列]"),
            (Wait,   "Wait  [数値]"),
            (Move,   "Move  [選択]"),
            (Repeat, "Scope  [入れ子]"),
        };
    }


    /// <summary>
    /// 入力値とブロック固有データの保存・復元を確かめるデモ．
    /// </summary>
    /// <remarks>
    /// 確かめられること．
    ///   - ヘッダーの入力値が Save / Load をまたいで維持される
    ///   - 入力値が Undo / Redo で戻る
    ///   - ブロック固有データ（色替え）が同じ経路で保存される
    ///   - 入れ子にしても、折り畳んでも中身が失われない
    ///
    /// [NOTE] UIはシーンに置かず実行時に構築する．既存デモと同じ方針．
    /// </remarks>
    public sealed class InputBlocksDemo : MonoBehaviour {

        [Header("Scene references")]
        [SerializeField] private BPG_ProgrammingEnv _workspace;
        [SerializeField] private RectTransform _canvasRoot;
        [SerializeField] private RectTransform _draggingLayer;

        [Header("Layout")]
        [SerializeField] private float _topBarHeight = 84f;
        [SerializeField] private float _paletteWidth = 300f;
        [SerializeField] private float _valuePanelWidth = 340f;
        [SerializeField] private float _themeBarHeight = 104f;

        private DemoTheme[] _themes;
        private int _themeIndex = 0;

        private Image _background;
        private Image _topBar;
        private Image _palette;
        private Image _valuePanel;
        private Image _themeBar;
        private Image _workspaceImage;
        private Text _titleText;
        private Text _statusText;
        private Text _hintText;
        private Text _valueText;
        private readonly List<Text> _headings = new();
        private readonly List<Image> _buttonImages = new();
        private readonly List<Text> _buttonLabels = new();
        private readonly List<Image> _themeButtons = new();
        private readonly List<Text> _themeButtonLabels = new();

        // 値の一覧を更新するため、購読中の入力を覚えておく
        private readonly List<I_BPG_BlockSectionHeaderInput> _watched = new();

        private int _spawnCount = 0;
        private BPG_UndoHistory _history;
        private DemoContextMenu _contextMenu;

        private string SavePath => BPG_BlockStorage.GetDefaultPath("demo-input-blocks");


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        private void Start() {
            if (_workspace == null || _canvasRoot == null) {
                Debug.LogError("Demo references are not assigned.", this);
                enabled = false;
                return;
            }

            _themes = DemoTheme.CreateAll();
            _history = new BPG_UndoHistory(_workspace);

            // ※復元でインスタンスが作り直されるため、見た目と購読をやり直す
            _history.OnRestored += _ => Refresh();

            BPG_BlockEventBus.OnStartDrag
                .Subscribe(_ => _history.Record("ブロックの移動"))
                .AddTo(this);

            BPG_BlockEventBus.OnEndDrag
                .Subscribe(_ => Refresh())
                .AddTo(this);

            BPG_BlockEventBus.OnSecondaryAction
                .Subscribe(OpenContextMenu)
                .AddTo(this);

            BuildUI();
            ApplyTheme(_themes[_themeIndex]);
            Refresh();

            SetStatus("ブロックを追加して値を入力し、Save → Clear → Load を試してください。");
        }


        /// ----------------------------------------------------------------------------
        // UI 構築

        private void BuildUI() {
            _background = DemoUIFactory.CreateImage("Background", _canvasRoot, raycastTarget: false);
            DemoUIFactory.Stretch(_background.rectTransform);

            _workspaceImage = _workspace.GetComponent<Image>();
            if (_workspaceImage == null) {
                _workspaceImage = _workspace.gameObject.AddComponent<Image>();
            }
            _workspaceImage.raycastTarget = true;
            DemoUIFactory.SetAnchored(_workspace.RectTransform,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: new Vector2(_paletteWidth, _themeBarHeight),
                offsetMax: new Vector2(-_valuePanelWidth, -_topBarHeight));

            BuildTopBar();
            BuildPalette();
            BuildValuePanel();
            BuildThemeBar();

            _contextMenu = new DemoContextMenu(_canvasRoot);

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
                "Input Blocks  Demo", 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            DemoUIFactory.SetAnchored(_titleText.rectTransform,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(24f, 0f), new Vector2(360f, 0f));

            var actions = new (string label, System.Action action)[] {
                ("Save",  Save),
                ("Load",  Load),
                ("Clear", Clear),
                ("Redo",  Redo),
                ("Undo",  Undo),
            };

            const float buttonWidth = 92f;
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
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(392f, 0f), new Vector2(-560f, 0f));
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

            const float buttonHeight = 58f;
            const float spacing = 12f;
            for (int i = 0; i < InputBlockCatalog.Items.Length; i++) {
                var item = InputBlockCatalog.Items[i];
                var button = DemoUIFactory.CreateButton($"Button ({item.label})", _palette.transform, item.label, 19);

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
                "入力欄をクリックして値を\n変えてください。\n\n"
                + "Save → Clear → Load で\n値ごと戻ります。\n\n"
                + "右クリック（スマホでは\n長押し）で色を変えると、\n"
                + "それも保存されます。",
                15, FontStyle.Normal, TextAnchor.LowerLeft);
            _hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            DemoUIFactory.SetAnchored(_hintText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(22f, 20f), new Vector2(-16f, 260f));
        }

        private void BuildValuePanel() {
            _valuePanel = DemoUIFactory.CreateImage("ValuePanel", _canvasRoot);
            _valuePanel.type = Image.Type.Sliced;
            DemoUIFactory.SetAnchored(_valuePanel.rectTransform,
                anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 1f),
                offsetMin: new Vector2(-_valuePanelWidth + 8f, _themeBarHeight + 2f),
                offsetMax: new Vector2(-8f, -_topBarHeight - 2f));

            var heading = DemoUIFactory.CreateText("Heading", _valuePanel.transform, "SAVED VALUES", 16,
                FontStyle.Bold, TextAnchor.MiddleLeft);
            DemoUIFactory.SetAnchored(heading.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(22f, -52f), new Vector2(-16f, -18f));
            _headings.Add(heading);

            // ※保存されるのと同じ値を、その場で見えるようにする
            _valueText = DemoUIFactory.CreateText("Values", _valuePanel.transform, "", 16,
                FontStyle.Normal, TextAnchor.UpperLeft);
            _valueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            DemoUIFactory.SetAnchored(_valueText.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(22f, 20f), new Vector2(-16f, -62f));
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

        public void Spawn(string prefabName) {
            var prefab = BPG_BlockUtils.LoadBlockPrefab(prefabName, _workspace);
            if (prefab == null) {
                SetStatus($"プレハブが見つかりません: {prefabName}");
                return;
            }

            _history.Record($"{prefabName} の追加");
            var block = BPG_BlockUtils.CreateBlock(prefab, _workspace);

            int step = _spawnCount++ % 8;
            block.RectTransform.anchoredPosition = new Vector2(70f + step * 24f, -70f - step * 30f);

            Refresh();
            SetStatus($"{prefabName} を追加しました。");
        }

        public void Save() {
            var roots = GetRootBlocks();
            BPG_BlockStorage.Save(SavePath, roots);
            SetStatus($"{roots.Count} 個のブロックを入力値ごと保存しました。 ({SavePath})");
        }

        public void Load() {
            if (!BPG_BlockStorage.Exists(SavePath)) {
                SetStatus("保存データがありません。先に Save を実行してください。");
                return;
            }

            _history.Record("読み込み");
            ClearBlocks();

            var restored = BPG_BlockStorage.Load(SavePath, _workspace);

            Refresh();
            SetStatus($"{restored.Count} 個のブロックを入力値ごと読み込みました。");
        }

        public void Clear() {
            int count = GetRootBlocks().Count;
            if (count > 0) {
                _history.Record("全消去");
            }
            ClearBlocks();
            Refresh();
            SetStatus(count > 0 ? $"{count} 個のブロックを削除しました。" : "ワークスペースは空です。");
        }

        public void Undo() {
            SetStatus(_history.Undo() ? "元に戻しました。" : "取り消せる操作がありません。");
        }

        public void Redo() {
            SetStatus(_history.Redo() ? "やり直しました。" : "やり直せる操作がありません。");
        }

        public void SelectTheme(int index) {
            if (index < 0 || index >= _themes.Length)
                return;

            _themeIndex = index;
            ApplyTheme(_themes[index]);
            SetStatus($"テーマを {_themes[index].Name} に切り替えました。");
        }


        /// ----------------------------------------------------------------------------
        // コンテキストメニュー

        private void OpenContextMenu(BlockPointerEvent e) {
            var block = e.Block;
            if (block == null)
                return;

            var section = block.GetFirstSection();
            var tint = block.RectTransform.GetComponent<DemoBlockTint>();

            var items = new List<(string, System.Action)> {
                ("複製", () => Duplicate(block)),
                ("削除", () => Remove(block)),
            };
            if (tint != null) {
                items.Insert(0, ("色を変える", () => ChangeTint(block, tint)));
            }
            if (section?.Body != null) {
                items.Add((section.IsCollapsed ? "展開" : "折り畳む", () => ToggleCollapse(section)));
            }

            _contextMenu.ApplyTheme(CurrentTheme);
            _contextMenu.Open(e.ScreenPosition, items);
        }

        private void ChangeTint(I_BPG_Block block, DemoBlockTint tint) {
            _history.Record("色の変更");
            tint.Next();

            ApplyThemeToBlocks(CurrentTheme);
            RefreshValuePanel();
            SetStatus(tint.HasTint
                ? "色を変えました。この色もブロック固有データとして保存されます。"
                : "テーマ既定の色に戻しました。");
        }

        public void Duplicate(I_BPG_Block block) {
            _history.Record("複製");

            var clone = BPG_BlockSerializer.Duplicate(block, _workspace);
            if (clone == null) {
                SetStatus("複製に失敗しました。");
                return;
            }

            clone.RectTransform.anchoredPosition =
                block.RectTransform.anchoredPosition + new Vector2(32f, -32f);

            Refresh();
            SetStatus("入力値と色ごと複製しました。");
        }

        public void Remove(I_BPG_Block block) {
            _history.Record("削除");

            int count = block.GetAllChildBlocksCount(containSelf: true);
            BPG_BlockUtils.RemoveBlock(block);
            block.RectTransform.gameObject.SetActive(false);

            Refresh();
            SetStatus($"{count} 個のブロックを削除しました。");
        }

        public void ToggleCollapse(I_BPG_BlockSection section) {
            _history.Record(section.IsCollapsed ? "展開" : "折り畳み");
            section.SetCollapsed(!section.IsCollapsed);
            SetStatus(section.IsCollapsed
                ? "折り畳みました。中身は保持されています。"
                : "展開しました。");
        }


        /// ----------------------------------------------------------------------------
        // 値の一覧

        /// <summary>
        /// 構成が変わった後の再構築．
        /// </summary>
        private void Refresh() {
            ApplyThemeToBlocks(CurrentTheme);
            WatchInputs();
            RefreshValuePanel();
        }

        /// <summary>
        /// 入力の変更を購読し直す．
        /// </summary>
        /// <remarks>
        /// [NOTE] 復元でインスタンスが作り直されるため、購読も張り直す必要がある．
        /// </remarks>
        private void WatchInputs() {
            foreach (var input in _watched) {
                if (input != null) {
                    input.OnValueChanged -= OnInputValueChanged;
                }
            }
            _watched.Clear();

            foreach (var input in EnumerateInputs()) {
                input.OnValueChanged += OnInputValueChanged;
                _watched.Add(input);
            }

            WatchEditStart();
        }

        /// <summary>
        /// 入力を触り始めた時点を履歴へ積めるようにする．
        /// </summary>
        /// <remarks>
        /// [NOTE] 値が変わってから記録したのでは、取り消し先が変更後の値になってしまう．
        ///        「編集を始める直前」を記録する必要があるため、確定ではなく開始を捉える．
        ///        値が変わらなければ同じ内容の記録になるが、履歴側で重複は捨てられる．
        /// </remarks>
        private void WatchEditStart() {
            foreach (var field in _workspace.RectTransform.GetComponentsInChildren<TMP_InputField>(true)) {
                field.onSelect.RemoveListener(OnInputFocused);
                field.onSelect.AddListener(OnInputFocused);
            }

            foreach (var dropdown in _workspace.RectTransform.GetComponentsInChildren<TMP_Dropdown>(true)) {
                // ※ドロップダウンには「開いた」を知る手段が無いため、押下を捉える
                if (dropdown.GetComponent<EventTrigger>() != null)
                    continue;

                var trigger = dropdown.gameObject.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entry.callback.AddListener(_ => _history.Record("選択の変更"));
                trigger.triggers.Add(entry);
            }
        }

        private void OnInputFocused(string value) {
            _history.Record("入力の変更");
        }

        private void OnInputValueChanged(I_BPG_BlockSectionHeaderInput input) {
            RefreshValuePanel();
        }

        private IEnumerable<I_BPG_BlockSectionHeaderInput> EnumerateInputs() {
            return _workspace.RectTransform
                .GetComponentsInChildren<BPG_BlockSectionHeader_InputBase>(true)
                .Cast<I_BPG_BlockSectionHeaderInput>();
        }

        /// <summary>
        /// 保存されるのと同じ内容を一覧表示する．
        /// </summary>
        private void RefreshValuePanel() {
            if (_valueText == null)
                return;

            var builder = new StringBuilder();
            foreach (var root in GetRootBlocks()) {
                AppendBlock(builder, root, depth: 0);
            }

            _valueText.text = (builder.Length > 0)
                ? builder.ToString()
                : "ブロックがありません。";
        }

        private static void AppendBlock(StringBuilder builder, I_BPG_Block block, int depth) {
            string indent = new string(' ', depth * 2);
            builder.Append(indent).Append("- ").Append(ShortName(block.RectTransform.name));

            var values = new List<string>();
            if (block.Layout != null) {
                foreach (var section in block.Layout.Sections) {
                    if (section?.Header == null) continue;

                    foreach (var input in section.Header.Inputs) {
                        values.Add(string.IsNullOrEmpty(input.Value) ? "(空)" : input.Value);
                    }
                }
            }
            if (values.Count > 0) {
                builder.Append(" : ").Append(string.Join(" / ", values));
            }

            var tint = block.RectTransform.GetComponent<DemoBlockTint>();
            if (tint != null && tint.HasTint) {
                builder.Append("  [色 ").Append(tint.Index).Append("]");
            }
            builder.AppendLine();

            if (block.Layout == null)
                return;

            foreach (var section in block.Layout.Sections) {
                if (section?.Body == null) continue;

                if (section.IsCollapsed) {
                    builder.Append(indent).Append("    (折り畳み中 ")
                        .Append(section.Body.ChildBlocks.Count).AppendLine(" 個)");
                }
                foreach (var child in section.Body.ChildBlocks) {
                    AppendBlock(builder, child, depth + 1);
                }
            }
        }

        /// <summary>
        /// "Block [Say]" から "Say" を取り出す．
        /// </summary>
        private static string ShortName(string prefabName) {
            int start = prefabName.IndexOf('[');
            int end = prefabName.IndexOf(']');
            return (0 <= start && start < end)
                ? prefabName.Substring(start + 1, end - start - 1)
                : prefabName;
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private DemoTheme CurrentTheme => _themes[_themeIndex];

        private IReadOnlyList<I_BPG_Block> GetRootBlocks() {
            var blocks = new List<I_BPG_Block>();
            foreach (Transform child in _workspace.RectTransform) {
                // [NOTE] Destroy はフレーム終端まで遅延するため、非アクティブ化を目印に除外する．
                if (child.gameObject.activeSelf && child.TryGetComponent<I_BPG_Block>(out var block)) {
                    blocks.Add(block);
                }
            }
            return blocks;
        }

        private void ClearBlocks() {
            foreach (var block in GetRootBlocks()) {
                Destroy(block.RectTransform.gameObject);
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

            _background.sprite = null;
            _background.color = Color.Lerp(theme.BackgroundTop, theme.BackgroundBottom, 0.5f);

            _workspaceImage.sprite = panelSprite;
            _workspaceImage.type = Image.Type.Sliced;
            _workspaceImage.color = Color.Lerp(theme.BackgroundBottom, theme.PanelColor, 0.25f);

            foreach (var panel in new[] { _topBar, _palette, _valuePanel, _themeBar }) {
                panel.sprite = panelSprite;
                panel.type = Image.Type.Sliced;
                panel.color = theme.PanelColor;
                ApplyOutline(panel.gameObject, theme);
            }

            _titleText.color = theme.TextColor;
            _statusText.color = theme.SubTextColor;
            _hintText.color = theme.SubTextColor;
            _valueText.color = theme.TextColor;
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
                // ※固有データの色替えがあればテーマ色より優先する
                var tint = layout.GetComponent<DemoBlockTint>();
                layout.Color = (tint != null && tint.HasTint)
                    ? tint.Color
                    : GetBlockColor(theme, layout.name);
                layout.SetLayoutDirty();

                foreach (var item in layout.GetComponentsInChildren<BPG_BlockSectionHeader_Item>(true)) {
                    if (item.TryGetComponent<Image>(out var image)) {
                        image.sprite = itemSprite;
                        image.type = Image.Type.Sliced;
                        image.color = theme.ItemColor;
                    }
                }

                // ラベルはテーマの文字色に合わせる
                foreach (var label in layout.GetComponentsInChildren<Text>(true)) {
                    label.color = theme.ButtonTextColor;
                }

                ApplyThemeToInputs(layout, theme);

                foreach (var shadow in layout.GetComponentsInChildren<Shadow>(true)) {
                    if (shadow is Outline)
                        continue;

                    shadow.enabled = theme.ShadowEnabled;
                    shadow.effectDistance = theme.ShadowDistance;
                    shadow.effectColor = theme.ShadowColor;
                }
            }
        }

        /// <summary>
        /// 入力欄の見た目をテーマに合わせる．
        /// </summary>
        private static void ApplyThemeToInputs(Component root, DemoTheme theme) {
            var fieldSprite = DemoUIFactory.GetRoundedSprite(theme.ItemCornerRadius);

            foreach (var field in root.GetComponentsInChildren<TMP_InputField>(true)) {
                if (field.TryGetComponent<Image>(out var image)) {
                    image.sprite = fieldSprite;
                    image.type = Image.Type.Sliced;
                    image.color = theme.PanelColor;
                }
                if (field.textComponent != null) {
                    field.textComponent.color = theme.TextColor;
                }
                if (field.placeholder is TMP_Text placeholder) {
                    placeholder.color = theme.SubTextColor;
                }
                field.selectionColor = new Color(theme.AccentColor.r, theme.AccentColor.g, theme.AccentColor.b, 0.4f);
                field.caretColor = theme.TextColor;
            }

            foreach (var dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true)) {
                if (dropdown.TryGetComponent<Image>(out var image)) {
                    image.sprite = fieldSprite;
                    image.type = Image.Type.Sliced;
                    image.color = theme.PanelColor;
                }
                if (dropdown.captionText != null) {
                    dropdown.captionText.color = theme.TextColor;
                }
                if (dropdown.itemText != null) {
                    dropdown.itemText.color = theme.TextColor;
                }
            }
        }

        /// <summary>
        /// プレハブ名に対応するブロック色を取得する．
        /// </summary>
        private static Color GetBlockColor(DemoTheme theme, string prefabName) {
            switch (prefabName) {
                case InputBlockCatalog.Entry: return theme.BlockEntry;
                case InputBlockCatalog.Say: return theme.BlockNormal;
                case InputBlockCatalog.Wait: return theme.BlockMultiScope;
                case InputBlockCatalog.Move: return theme.BlockScope;
                case InputBlockCatalog.Repeat: return theme.BlockScope;
                default: return theme.BlockNormal;
            }
        }
    }
}
