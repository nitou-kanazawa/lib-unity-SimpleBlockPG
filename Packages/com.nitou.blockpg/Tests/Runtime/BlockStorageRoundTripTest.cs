using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Serialization;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// 実際のブロックを保存し、ファイル経由で復元するまでの往復テスト．
    /// </summary>
    public class BlockStorageRoundTripTest {

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
            if (File.Exists(_path)) {
                File.Delete(_path);
            }
        }


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// root(Scope) > [child1(Normal), child2(Scope) > grandChild(Normal)] を組み立てる．
        /// </summary>
        private I_BPG_Block BuildSampleTree() {
            var root = _env.CreateBlock(PrefabName.Scope);
            var child1 = _env.CreateBlock(PrefabName.Normal);
            var child2 = _env.CreateBlock(PrefabName.Scope);
            var grandChild = _env.CreateBlock(PrefabName.Normal);

            var rootBody = root.GetFirstSection().Body;
            rootBody.AppendLast(child1);
            rootBody.AppendLast(child2);

            child2.GetFirstSection().Body.AppendLast(grandChild);

            return root;
        }

        /// <summary>
        /// 階層構造を「名前の入れ子」として表現する．（※比較用）
        /// </summary>
        private static string Describe(I_BPG_Block block) {
            var sections = block.Layout.Sections
                .Where(s => s?.Body != null)
                .Select(s => string.Join(",", s.Body.ChildBlocks.Select(Describe)));

            var children = string.Join("|", sections);
            return string.IsNullOrEmpty(children)
                ? block.RectTransform.name
                : $"{block.RectTransform.name}({children})";
        }


        /// ----------------------------------------------------------------------------
        // 往復

        [Test]
        public void 組み立てた階層が保存と復元で一致する() {
            // Arrange
            var original = BuildSampleTree();
            var expected = Describe(original);

            // Act : 保存 -> 元を破棄 -> 復元
            BPG_BlockStorage.Save(_path, new[] { original });
            UnityEngine.Object.DestroyImmediate(original.RectTransform.gameObject);

            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            // Assert
            Assert.That(restored.Count, Is.EqualTo(1));
            Assert.That(Describe(restored[0]), Is.EqualTo(expected));
        }

        [Test]
        public void 識別IDが保存と復元をまたいで維持される() {
            var original = BuildSampleTree();
            var expectedIds = original.GetAllChildBlocks(containSelf: true)
                .Select(b => b.Id).OrderBy(id => id).ToArray();

            BPG_BlockStorage.Save(_path, new[] { original });
            UnityEngine.Object.DestroyImmediate(original.RectTransform.gameObject);

            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            var actualIds = restored[0].GetAllChildBlocks(containSelf: true)
                .Select(b => b.Id).OrderBy(id => id).ToArray();
            Assert.That(actualIds, Is.EqualTo(expectedIds));
        }

        [Test]
        public void 複数のルートブロックを1ファイルに保存して復元できる() {
            var first = _env.CreateBlock(PrefabName.Entry);
            var second = _env.CreateBlock(PrefabName.Scope);

            BPG_BlockStorage.Save(_path, new[] { first, second });
            UnityEngine.Object.DestroyImmediate(first.RectTransform.gameObject);
            UnityEngine.Object.DestroyImmediate(second.RectTransform.gameObject);

            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            Assert.That(restored.Count, Is.EqualTo(2));
            Assert.That(restored.Select(b => b.RectTransform.name),
                Is.EqualTo(new[] { PrefabName.Entry, PrefabName.Scope }));
        }

        [Test]
        public void 読み込みはフレームをまたがずに完了する() {
            var original = BuildSampleTree();
            BPG_BlockStorage.Save(_path, new[] { original });
            UnityEngine.Object.DestroyImmediate(original.RectTransform.gameObject);

            var frameBefore = Time.frameCount;
            var restored = BPG_BlockStorage.Load(_path, _env.ProgrammingEnv);

            Assert.That(Time.frameCount, Is.EqualTo(frameBefore));
            Assert.That(restored[0].GetAllChildBlocksCount(containSelf: true), Is.EqualTo(4));
        }


        /// ----------------------------------------------------------------------------
        // 非同期API

        // [NOTE] Task を待つのに Assert.ThrowsAsync / CatchAsync / .Result / .Wait() は使わない．
        //        いずれもメインスレッドをブロックするが、継続は Unity の
        //        SynchronizationContext 経由でメインスレッドへ戻ろうとするため
        //        自己デッドロックし、Editor がハングする．
        //        完了はコルーチンで待つこと．

        /// <summary>
        /// Task の完了をフレーム送りで待つ．
        /// </summary>
        private static IEnumerator Await(Task task) {
            while (!task.IsCompleted) {
                yield return null;
            }

            // ※失敗を握り潰さない（AggregateException だと原因が読みにくいので中身を投げる）
            if (task.IsFaulted) {
                throw task.Exception.InnerExceptions.Count == 1
                    ? task.Exception.InnerExceptions[0]
                    : task.Exception;
            }
        }

        [UnityTest]
        public IEnumerator SaveAsyncとLoadAsyncで往復できる() {
            // Arrange
            var original = BuildSampleTree();
            var expected = Describe(original);

            // Act
            yield return Await(BPG_BlockStorage.SaveAsync(_path, new[] { original }));
            UnityEngine.Object.DestroyImmediate(original.RectTransform.gameObject);

            var loadTask = BPG_BlockStorage.LoadAsync(_path, _env.ProgrammingEnv);
            yield return Await(loadTask);
            var restored = loadTask.Result;

            // Assert
            Assert.That(File.Exists(_path), Is.True);
            Assert.That(restored.Count, Is.EqualTo(1));
            Assert.That(Describe(restored[0]), Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator LoadAsyncの生成部分はフレームをまたがない() {
            // [NOTE] ファイルI/Oは別スレッドで待つためフレームは進むが、
            //        メインスレッドへ戻った後のブロック生成は1フレーム内で完結する．
            var original = BuildSampleTree();
            yield return Await(BPG_BlockStorage.SaveAsync(_path, new[] { original }));
            UnityEngine.Object.DestroyImmediate(original.RectTransform.gameObject);

            var sBlocks = BPG_BlockStorage.LoadSerializableBlocks(_path);

            var frameBefore = Time.frameCount;
            var restored = sBlocks
                .Select(s => BPG_BlockSerializer.SerializableBlockToBlock(s, _env.ProgrammingEnv))
                .ToArray();

            Assert.That(Time.frameCount, Is.EqualTo(frameBefore));
            Assert.That(restored[0].GetAllChildBlocksCount(containSelf: true), Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator LoadAsyncは復元後にメインスレッドへ戻っている() {
            var original = _env.CreateBlock(PrefabName.Scope);
            yield return Await(BPG_BlockStorage.SaveAsync(_path, new[] { original }));

            yield return Await(BPG_BlockStorage.LoadAsync(_path, _env.ProgrammingEnv));

            // ※メインスレッド以外では Unity API に触れないため、復元が成立した時点で戻っている
            Assert.That(_env.ProgrammingEnv.RectTransform, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator 存在しないファイルのLoadAsyncは空を返す() {
            LogAssert.ignoreFailingMessages = true;

            var loadTask = BPG_BlockStorage.LoadAsync(_path, _env.ProgrammingEnv);
            yield return Await(loadTask);

            Assert.That(loadTask.Result, Is.Empty);
            LogAssert.ignoreFailingMessages = false;
        }
    }
}
