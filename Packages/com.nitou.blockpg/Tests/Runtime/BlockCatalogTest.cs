using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ブロックの解決と生成の差し替えを検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] 以前は <c>Resources.Load</c> が静的クラスに直結しており、差し替え手段が無かった．
    ///        そのため生成を伴うロジックは全て実資産に依存し、資産が動くとテストが壊れた．
    /// </remarks>
    public class BlockCatalogTest {

        private BlockPGTestEnv _env;
        private TestBlockCatalog _catalog;

        [SetUp]
        public void SetUp() {
            _env = new BlockPGTestEnv();
            _catalog = new TestBlockCatalog();
        }

        [TearDown]
        public void TearDown() {
            _catalog.Dispose();
            _env.Dispose();
        }


        /// ----------------------------------------------------------------------------
        // 既定の挙動

        [Test]
        public void 既定ではResourcesから引かれる() {
            var catalog = _env.Env.BlockCatalog;

            Assert.That(catalog, Is.InstanceOf<BPG_ResourcesBlockCatalog>());
            Assert.That(catalog.GetPrefab(PrefabName.Normal), Is.Not.Null);
        }

        [Test]
        public void 既定のファクトリが使われる() {
            Assert.That(_env.Env.BlockFactory, Is.InstanceOf<BPG_DefaultBlockFactory>());
        }

        [Test]
        public void 差し替えをnullで戻せる() {
            _env.Env.SetBlockCatalog(_catalog);
            Assert.That(_env.Env.BlockCatalog, Is.SameAs(_catalog), "前提: 差し替わっていること");

            _env.Env.SetBlockCatalog(null);

            Assert.That(_env.Env.BlockCatalog, Is.InstanceOf<BPG_ResourcesBlockCatalog>());
        }

        [Test]
        public void 読み込み先のフォルダを変えられる() {
            var catalog = new BPG_ResourcesBlockCatalog("SomeOtherFolder");
            Assert.That(catalog.FolderPath, Is.EqualTo("SomeOtherFolder"));

            LogAssert.Expect(LogType.Warning, new Regex("Block prefab is not found"));
            Assert.That(catalog.GetPrefab(PrefabName.Normal), Is.Null);
        }


        /// ----------------------------------------------------------------------------
        // カタログの差し替え

        [Test]
        public void 差し替えたカタログからプレハブが引かれる() {
            _catalog.Register("Dummy");
            _env.Env.SetBlockCatalog(_catalog);

            var prefab = BPG_BlockUtils.LoadBlockPrefab("Dummy", _env.ProgrammingEnv);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(_catalog.RequestedNames, Does.Contain("Dummy"));
        }

        [Test]
        public void 実資産に依存せずブロックを生成できる() {
            // [NOTE] Resources に存在しない名前でも、カタログを差し替えれば生成できる．
            var prefab = _catalog.Register("Dummy");
            _env.Env.SetBlockCatalog(_catalog);

            var block = BPG_BlockUtils.CreateBlock(prefab, _env.ProgrammingEnv);

            Assert.That(block, Is.Not.Null);
            Assert.That(block.RectTransform.name, Is.EqualTo("Dummy"), "プレハブ名がそのまま付くはず．");
            Assert.That(block.Layout, Is.Not.Null, "生成の時点で参照が解決しているはず．");
            Assert.That(_env.ProgrammingEnv.GetRootBlocks(), Has.Count.EqualTo(1));
        }

        [Test]
        public void 実資産に依存せず復元できる() {
            // [NOTE] これが #33 の狙い．保存データはブロック名しか持たないため、
            //        名前からプレハブを引く経路が差し替えられないと復元を検証できない．
            _catalog.Register("Dummy");
            _env.Env.SetBlockCatalog(_catalog);

            var data = new SerializableBlock("saved-id", "Dummy", new Vector3(12f, -34f, 0f));
            data.sections.Add(new SerializableBlockSection());

            var restored = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.Id, Is.EqualTo("saved-id"));
            Assert.That(restored.RectTransform.localPosition, Is.EqualTo(new Vector3(12f, -34f, 0f)));
            Assert.That(_catalog.RequestedNames, Does.Contain("Dummy"));
        }

        [Test]
        public void カタログが見つけられないと生成されない() {
            _env.Env.SetBlockCatalog(_catalog);

            var prefab = BPG_BlockUtils.LoadBlockPrefab("Unknown", _env.ProgrammingEnv);

            Assert.That(prefab, Is.Null);
            Assert.That(_catalog.RequestedNames, Does.Contain("Unknown"));
        }

        [Test]
        public void 見つからないブロックは復元をとばして続行する() {
            _catalog.Register("Dummy");
            _env.Env.SetBlockCatalog(_catalog);

            var data = new SerializableBlock("root", "Unknown", Vector3.zero);
            data.sections.Add(new SerializableBlockSection());

            var restored = BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);

            Assert.That(restored, Is.Null, "見つからない場合は null を返すはず．");
            Assert.That(_env.ProgrammingEnv.GetRootBlocks(), Is.Empty);
        }


        /// ----------------------------------------------------------------------------
        // ファクトリの差し替え

        [Test]
        public void 差し替えたファクトリが使われる() {
            var factory = new TestBlockFactory();
            _env.Env.SetBlockFactory(factory);

            _env.CreateBlock(PrefabName.Normal);
            _env.CreateBlock(PrefabName.Scope);

            Assert.That(factory.CreatedCount, Is.EqualTo(2));
        }

        [Test]
        public void ファクトリが失敗すると生成されない() {
            var factory = new TestBlockFactory { ShouldFail = true };
            _env.Env.SetBlockFactory(factory);

            var prefab = BPG_BlockUtils.LoadBlockPrefab(PrefabName.Normal, _env.ProgrammingEnv);

            LogAssert.Expect(LogType.Warning, new Regex("Failed to create block"));
            var block = BPG_BlockUtils.CreateBlock(prefab, _env.ProgrammingEnv);

            Assert.That(block, Is.Null);
            Assert.That(_env.ProgrammingEnv.GetRootBlocks(), Is.Empty);
        }

        [Test]
        public void 復元でもファクトリが使われる() {
            // [NOTE] プールを挟む実装が復元経路をすり抜けないことを押さえる．
            var factory = new TestBlockFactory();
            _catalog.Register("Dummy");
            _env.Env.SetBlockCatalog(_catalog);
            _env.Env.SetBlockFactory(factory);

            var data = new SerializableBlock("root", "Dummy", Vector3.zero);
            data.sections.Add(new SerializableBlockSection());
            BPG_BlockSerializer.SerializableBlockToBlock(data, _env.ProgrammingEnv);

            Assert.That(factory.CreatedCount, Is.EqualTo(1));
        }
    }
}
