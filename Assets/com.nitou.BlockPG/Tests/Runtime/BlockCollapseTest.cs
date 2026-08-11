using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// セクションの折り畳みを検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] 折り畳みはボディの非表示で実現する．子ブロックを個別に非表示にすると
    ///        <c>ChildBlocks</c> から外れ、保存時に中身が失われる．
    ///        この差はテストで明示的に押さえる．
    /// </remarks>
    public class BlockCollapseTest {

        private BlockPGTestEnv _env;
        private string _path;

        [SetUp]
        public void SetUp() {
            _env = new BlockPGTestEnv();
            _path = Path.Combine(Application.temporaryCachePath, "BlockPGTests", $"{Guid.NewGuid():N}.xml");
        }

        [TearDown]
        public void TearDown() {
            _env.Dispose();
            if (File.Exists(_path)) File.Delete(_path);
        }


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// 子ブロックを2つ持つ Scope を作る．
        /// </summary>
        private I_BPG_Block BuildScopeWithChildren() {
            var scope = _env.CreateBlock(PrefabName.Scope);
            var body = scope.GetFirstSection().Body;
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            scope.Layout.UpdateLayout();
            return scope;
        }


        /// ----------------------------------------------------------------------------
        // 基本動作

        [Test]
        public void 既定では折り畳まれていない() {
            var scope = _env.CreateBlock(PrefabName.Scope);

            Assert.That(scope.GetFirstSection().IsCollapsed, Is.False);
        }

        [Test]
        public void 折り畳むとブロックが縮む() {
            var scope = BuildScopeWithChildren();
            var section = scope.GetFirstSection();
            float expanded = scope.RectTransform.sizeDelta.y;

            section.SetCollapsed(true);
            scope.Layout.UpdateLayout();

            Assert.That(section.IsCollapsed, Is.True);
            Assert.That(scope.RectTransform.sizeDelta.y, Is.LessThan(expanded));

            // ※ヘッダーのぶんだけが残る
            Assert.That(scope.RectTransform.sizeDelta.y,
                Is.EqualTo(section.Header.Size.y).Within(0.01f));
        }

        [Test]
        public void 展開すると元のサイズに戻る() {
            var scope = BuildScopeWithChildren();
            var section = scope.GetFirstSection();
            var expanded = scope.RectTransform.sizeDelta;

            section.SetCollapsed(true);
            scope.Layout.UpdateLayout();
            section.SetCollapsed(false);
            scope.Layout.UpdateLayout();

            Assert.That(section.IsCollapsed, Is.False);
            Assert.That(scope.RectTransform.sizeDelta, Is.EqualTo(expanded));
        }

        [Test]
        public void 折り畳んでも子ブロックは保持される() {
            var scope = BuildScopeWithChildren();
            var section = scope.GetFirstSection();

            section.SetCollapsed(true);
            scope.Layout.UpdateLayout();

            Assert.That(section.Body.ChildBlocks, Has.Count.EqualTo(2));
            Assert.That(scope.GetAllChaildBlocksCount(containSelf: true), Is.EqualTo(3));
        }

        [Test]
        public void ボディを持たないセクションは折り畳めない() {
            // [NOTE] Normal ブロックのセクションは Body を持たない．
            var block = _env.CreateBlock(PrefabName.Normal);
            var section = block.GetFirstSection();

            Assert.DoesNotThrow(() => section.SetCollapsed(true));
            Assert.That(section.IsCollapsed, Is.False);
        }

        [Test]
        public void 同じ状態を設定しても副作用が無い() {
            var scope = BuildScopeWithChildren();
            var section = scope.GetFirstSection();
            var size = scope.RectTransform.sizeDelta;

            section.SetCollapsed(false);
            scope.Layout.UpdateLayout();

            Assert.That(scope.RectTransform.sizeDelta, Is.EqualTo(size));
        }


        /// ----------------------------------------------------------------------------
        // 保存と復元

        [Test]
        public void 折り畳んだ状態で保存しても子ブロックが失われない() {
            var scope = BuildScopeWithChildren();
            scope.GetFirstSection().SetCollapsed(true);
            scope.Layout.UpdateLayout();

            BPG_BlockStorage.Save(_path, new[] { scope });
            UnityEngine.Object.DestroyImmediate(scope.RectTransform.gameObject);

            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            Assert.That(restored, Has.Count.EqualTo(1));
            Assert.That(restored[0].GetAllChaildBlocksCount(containSelf: true), Is.EqualTo(3),
                "折り畳んだ状態で保存すると中身が失われている．");
        }

        [Test]
        public void 折り畳み状態が保存と復元で維持される() {
            var scope = BuildScopeWithChildren();
            scope.GetFirstSection().SetCollapsed(true);
            scope.Layout.UpdateLayout();

            BPG_BlockStorage.Save(_path, new[] { scope });
            UnityEngine.Object.DestroyImmediate(scope.RectTransform.gameObject);

            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            Assert.That(restored[0].GetFirstSection().IsCollapsed, Is.True);
        }

        [Test]
        public void 展開状態も保存と復元で維持される() {
            var scope = BuildScopeWithChildren();

            BPG_BlockStorage.Save(_path, new[] { scope });
            UnityEngine.Object.DestroyImmediate(scope.RectTransform.gameObject);

            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            Assert.That(restored[0].GetFirstSection().IsCollapsed, Is.False);
        }

        [Test]
        public void 折り畳み対応前のデータでも展開状態として読める() {
            // [NOTE] isCollapsed 要素が無い旧データとの互換．
            var data = new SerializableBlock("root", PrefabName.Scope, Vector3.zero);
            var section = new SerializableBlockSection();
            section.childBlocks.Add(new SerializableBlock("child", PrefabName.Normal, Vector3.zero));
            data.sections.Add(section);

            var xml = SerializableBlock.ToXElement(data);
            foreach (var stale in xml.Descendants("isCollapsed").ToArray()) {
                stale.Remove();
            }

            var parsed = SerializableBlock.FromXElement(xml);
            Assert.That(parsed.sections[0].isCollapsed, Is.False);

            var restored = BPG_BlockSerializer.SerializableBlockToBlock(parsed, _env.ProgrammingEnv);
            Assert.That(restored.GetFirstSection().IsCollapsed, Is.False);
        }


        /// ----------------------------------------------------------------------------
        // 複数セクション

        [Test]
        public void セクションごとに独立して折り畳める() {
            var multi = _env.CreateBlock(PrefabName.MultiScope);
            multi.Layout.Sections[0].Body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            multi.Layout.Sections[1].Body.AppendLast(_env.CreateBlock(PrefabName.Normal));
            multi.Layout.UpdateLayout();

            multi.Layout.Sections[0].SetCollapsed(true);
            multi.Layout.UpdateLayout();

            Assert.That(multi.Layout.Sections[0].IsCollapsed, Is.True);
            Assert.That(multi.Layout.Sections[1].IsCollapsed, Is.False);
        }
    }
}
