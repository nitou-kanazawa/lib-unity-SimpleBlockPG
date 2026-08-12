using UnityEngine;

namespace nitou.BlockPG.Demo {

    /// <summary>
    /// デモの見た目を定義する．
    /// [NOTE] ブロック本体のスプライトは凹凸（接続部の形状）を含む9スライス画像のため差し替えない．
    ///        配色・角丸・影・枠線の組み合わせで見た目を作る．
    /// </summary>
    public sealed class DemoTheme {

        public string Name;

        // 背景
        public Color BackgroundTop;
        public Color BackgroundBottom;

        // パネル（パレット・ツールバー）
        public Color PanelColor;
        public int PanelCornerRadius;
        public Color PanelOutlineColor;
        public int PanelOutlineWidth;

        // 文字とアクセント
        public Color TextColor;
        public Color SubTextColor;
        public Color AccentColor;
        public Color ButtonColor;
        public Color ButtonTextColor;
        public int ButtonCornerRadius;

        // ブロック
        public Color BlockEntry;
        public Color BlockNormal;
        public Color BlockScope;
        public Color BlockMultiScope;

        // ブロック内のラベル枠
        public Color ItemColor;
        public int ItemCornerRadius;

        // 影
        public bool ShadowEnabled;
        public Vector2 ShadowDistance;
        public Color ShadowColor;


        /// <summary>
        /// プレハブ名に対応するブロック色を取得する．
        /// </summary>
        public Color GetBlockColor(string prefabName) {
            switch (prefabName) {
                case DemoBlockCatalog.Entry: return BlockEntry;
                case DemoBlockCatalog.Normal: return BlockNormal;
                case DemoBlockCatalog.Scope: return BlockScope;
                case DemoBlockCatalog.MultiScope: return BlockMultiScope;
                default: return BlockNormal;
            }
        }


        /// ----------------------------------------------------------------------------
        // プリセット

        private static Color RGB(int r, int g, int b, float a = 1f) {
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }

        /// <summary>
        /// 用意されている全テーマ．
        /// </summary>
        public static DemoTheme[] CreateAll() {
            return new[] { Scratch(), Midnight(), Pastel(), Paper(), Terminal() };
        }

        /// <summary>
        /// 王道のブロックプログラミング風．彩度の高い原色と柔らかい影．
        /// </summary>
        public static DemoTheme Scratch() => new DemoTheme {
            Name = "Scratch",
            BackgroundTop = RGB(245, 247, 250),
            BackgroundBottom = RGB(226, 232, 240),
            PanelColor = RGB(255, 255, 255),
            PanelCornerRadius = 16,
            PanelOutlineColor = RGB(203, 213, 225),
            PanelOutlineWidth = 2,
            TextColor = RGB(30, 41, 59),
            SubTextColor = RGB(100, 116, 139),
            AccentColor = RGB(255, 140, 26),
            ButtonColor = RGB(255, 255, 255),
            ButtonTextColor = RGB(30, 41, 59),
            ButtonCornerRadius = 10,
            BlockEntry = RGB(255, 191, 0),
            BlockNormal = RGB(76, 151, 255),
            BlockScope = RGB(255, 140, 26),
            BlockMultiScope = RGB(64, 191, 111),
            ItemColor = RGB(0, 0, 0, 0.22f),
            ItemCornerRadius = 12,
            ShadowEnabled = true,
            ShadowDistance = new Vector2(2f, -2f),
            ShadowColor = RGB(0, 0, 0, 0.35f),
        };

        /// <summary>
        /// 暗色背景にネオン系の発色．影は落とさず枠線で見せる．
        /// </summary>
        public static DemoTheme Midnight() => new DemoTheme {
            Name = "Midnight",
            BackgroundTop = RGB(15, 23, 42),
            BackgroundBottom = RGB(2, 6, 23),
            PanelColor = RGB(30, 41, 59),
            PanelCornerRadius = 14,
            PanelOutlineColor = RGB(99, 102, 241),
            PanelOutlineWidth = 2,
            TextColor = RGB(226, 232, 240),
            SubTextColor = RGB(148, 163, 184),
            AccentColor = RGB(129, 140, 248),
            ButtonColor = RGB(51, 65, 85),
            ButtonTextColor = RGB(226, 232, 240),
            ButtonCornerRadius = 8,
            BlockEntry = RGB(244, 114, 182),
            BlockNormal = RGB(56, 189, 248),
            BlockScope = RGB(129, 140, 248),
            BlockMultiScope = RGB(52, 211, 153),
            ItemColor = RGB(255, 255, 255, 0.20f),
            ItemCornerRadius = 10,
            ShadowEnabled = false,
            ShadowDistance = Vector2.zero,
            ShadowColor = Color.clear,
        };

        /// <summary>
        /// 低彩度でやわらかい配色．角丸を大きく取る．
        /// </summary>
        public static DemoTheme Pastel() => new DemoTheme {
            Name = "Pastel",
            BackgroundTop = RGB(255, 247, 246),
            BackgroundBottom = RGB(240, 244, 255),
            PanelColor = RGB(255, 255, 255, 0.86f),
            PanelCornerRadius = 26,
            PanelOutlineColor = RGB(255, 214, 224),
            PanelOutlineWidth = 3,
            TextColor = RGB(92, 76, 92),
            SubTextColor = RGB(150, 134, 150),
            AccentColor = RGB(247, 168, 184),
            ButtonColor = RGB(255, 252, 253),
            ButtonTextColor = RGB(92, 76, 92),
            ButtonCornerRadius = 18,
            BlockEntry = RGB(255, 205, 178),
            BlockNormal = RGB(178, 217, 255),
            BlockScope = RGB(226, 190, 255),
            BlockMultiScope = RGB(183, 232, 204),
            ItemColor = RGB(255, 255, 255, 0.62f),
            ItemCornerRadius = 16,
            ShadowEnabled = true,
            ShadowDistance = new Vector2(0f, -3f),
            ShadowColor = RGB(180, 160, 180, 0.30f),
        };

        /// <summary>
        /// 紙とインク．影を使わず太い輪郭で構成する．
        /// </summary>
        public static DemoTheme Paper() => new DemoTheme {
            Name = "Paper",
            BackgroundTop = RGB(250, 246, 236),
            BackgroundBottom = RGB(240, 233, 218),
            PanelColor = RGB(253, 251, 245),
            PanelCornerRadius = 4,
            PanelOutlineColor = RGB(38, 34, 30),
            PanelOutlineWidth = 4,
            TextColor = RGB(38, 34, 30),
            SubTextColor = RGB(120, 110, 96),
            AccentColor = RGB(198, 62, 45),
            ButtonColor = RGB(253, 251, 245),
            ButtonTextColor = RGB(38, 34, 30),
            ButtonCornerRadius = 2,
            BlockEntry = RGB(232, 178, 70),
            BlockNormal = RGB(126, 164, 180),
            BlockScope = RGB(198, 62, 45),
            BlockMultiScope = RGB(122, 158, 118),
            ItemColor = RGB(38, 34, 30, 0.16f),
            ItemCornerRadius = 2,
            ShadowEnabled = false,
            ShadowDistance = Vector2.zero,
            ShadowColor = Color.clear,
        };

        /// <summary>
        /// 端末風．角を落として蛍光色を並べる．
        /// </summary>
        public static DemoTheme Terminal() => new DemoTheme {
            Name = "Terminal",
            BackgroundTop = RGB(10, 14, 12),
            BackgroundBottom = RGB(6, 10, 8),
            PanelColor = RGB(16, 24, 20),
            PanelCornerRadius = 0,
            PanelOutlineColor = RGB(57, 255, 143),
            PanelOutlineWidth = 2,
            TextColor = RGB(57, 255, 143),
            SubTextColor = RGB(32, 150, 88),
            AccentColor = RGB(57, 255, 143),
            ButtonColor = RGB(10, 18, 14),
            ButtonTextColor = RGB(57, 255, 143),
            ButtonCornerRadius = 0,
            BlockEntry = RGB(255, 221, 51),
            BlockNormal = RGB(57, 255, 143),
            BlockScope = RGB(51, 209, 255),
            BlockMultiScope = RGB(255, 108, 108),
            ItemColor = RGB(0, 0, 0, 0.45f),
            ItemCornerRadius = 0,
            ShadowEnabled = false,
            ShadowDistance = Vector2.zero,
            ShadowColor = Color.clear,
        };
    }
}
