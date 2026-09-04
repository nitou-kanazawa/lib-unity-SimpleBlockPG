using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.Blocks.Section;

namespace EditorTests {

    /// <summary>
    /// ブロック階層に残った LayoutGroup の検出を検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] 検出対象は「ライブラリが配置を決める役割」を持つオブジェクトだけ．
    ///        ヘッダーアイテムの内側などで利用者が LayoutGroup を使うのは正当なので、
    ///        階層まるごとを対象にすると誤検出になる．その線引きをここで押さえる．
    /// </remarks>
    public class LayoutGroupGuardTest {

        private GameObject _root;

        [SetUp]
        public void SetUp() {
            // ※非アクティブのまま組む（Awake を走らせずに構造だけ作る）
            _root = new GameObject("Block", typeof(RectTransform));
            _root.SetActive(false);
        }

        [TearDown]
        public void TearDown() {
            if (_root != null) Object.DestroyImmediate(_root);
        }


        /// ----------------------------------------------------------------------------
        // Helper

        private GameObject AddChild(GameObject parent, string name) {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        /// <summary>
        /// Block > Section > Header / Body の最小構成を組む．
        /// </summary>
        private (GameObject section, GameObject header, GameObject body) BuildBlock() {
            _root.AddComponent<BPG_Block>();
            _root.AddComponent<BPG_BlockVerticalLayout>();

            var section = AddChild(_root, "Section");
            var sectionComponent = section.AddComponent<BPG_BlockSection>();

            var header = AddChild(section, "Header");
            header.AddComponent<Image>();
            var headerComponent = header.AddComponent<BPG_BlockSectionHeader>();

            var body = AddChild(section, "Body");
            body.AddComponent<Image>();
            var bodyComponent = body.AddComponent<BPG_BlockSectionBody>();

            SetPrivateField(sectionComponent, "_header", headerComponent);
            SetPrivateField(sectionComponent, "_body", bodyComponent);

            return (section, header, body);
        }

        private static void SetPrivateField(object target, string name, object value) {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }


        /// ----------------------------------------------------------------------------
        // 役割の判定

        [Test]
        public void ブロックのルートは配置の担い手として扱う() {
            BuildBlock();

            Assert.That(BPG_LayoutGroupGuard.IsLayoutOwner(_root), Is.True);
        }

        [Test]
        public void セクションとヘッダーとボディも担い手として扱う() {
            var (section, header, body) = BuildBlock();

            Assert.That(BPG_LayoutGroupGuard.IsLayoutOwner(section), Is.True, "Section");
            Assert.That(BPG_LayoutGroupGuard.IsLayoutOwner(header), Is.True, "Header");
            Assert.That(BPG_LayoutGroupGuard.IsLayoutOwner(body), Is.True, "Body");
        }

        [Test]
        public void 関係ないオブジェクトは担い手ではない() {
            BuildBlock();
            var other = AddChild(_root, "Decoration");

            Assert.That(BPG_LayoutGroupGuard.IsLayoutOwner(other), Is.False);
            Assert.That(BPG_LayoutGroupGuard.IsLayoutOwner(null), Is.False);
        }


        /// ----------------------------------------------------------------------------
        // 検出

        [Test]
        public void 競合が無ければ空を返す() {
            BuildBlock();

            Assert.That(BPG_LayoutGroupGuard.Collect(_root), Is.Empty);
            Assert.That(BPG_LayoutGroupGuard.HasConflict(_root), Is.False);
        }

        [Test]
        public void ブロックのルートに付いたLayoutGroupを検出する() {
            BuildBlock();
            _root.AddComponent<VerticalLayoutGroup>();

            Assert.That(BPG_LayoutGroupGuard.Collect(_root), Has.Count.EqualTo(1));
            Assert.That(BPG_LayoutGroupGuard.HasConflict(_root), Is.True);
        }

        [Test]
        public void セクションとヘッダーとボディのLayoutGroupを検出する() {
            var (section, header, body) = BuildBlock();
            section.AddComponent<VerticalLayoutGroup>();
            header.AddComponent<HorizontalLayoutGroup>();
            body.AddComponent<VerticalLayoutGroup>();

            Assert.That(BPG_LayoutGroupGuard.Collect(_root), Has.Count.EqualTo(3));
        }

        [Test]
        public void ヘッダーアイテムの内側は検出しない() {
            // [NOTE] 利用者がアイテムの中で LayoutGroup を使うのは正当な用途．
            var (_, header, _) = BuildBlock();
            var item = AddChild(header, "Item");
            item.AddComponent<BPG_BlockSectionHeader_Item>();
            var inner = AddChild(item, "Inner");
            inner.AddComponent<HorizontalLayoutGroup>();

            Assert.That(BPG_LayoutGroupGuard.Collect(_root), Is.Empty,
                "アイテムの内側まで検出すると誤検出になる．");
        }

        [Test]
        public void 非アクティブなオブジェクトも検出する() {
            var (section, _, _) = BuildBlock();
            section.AddComponent<VerticalLayoutGroup>();
            section.SetActive(false);

            Assert.That(BPG_LayoutGroupGuard.Collect(_root), Has.Count.EqualTo(1));
        }

        [Test]
        public void 対象が無い場合も落ちない() {
            Assert.That(BPG_LayoutGroupGuard.Collect(null), Is.Empty);
            Assert.That(BPG_LayoutGroupGuard.HasConflict(null), Is.False);
        }


        /// ----------------------------------------------------------------------------
        // 同梱プレハブ

        [TestCase("Block [Entry]")]
        [TestCase("Block [Normal]")]
        [TestCase("Block [Scope]")]
        [TestCase("Block [MultiScope]")]
        public void 同梱プレハブは競合しない(string prefabName) {
            var prefab = Resources.Load<GameObject>($"BlockPG/{prefabName}");
            Assert.That(prefab, Is.Not.Null, "前提: プレハブが存在すること");

            Assert.That(BPG_LayoutGroupGuard.Collect(prefab), Is.Empty);
        }
    }
}
