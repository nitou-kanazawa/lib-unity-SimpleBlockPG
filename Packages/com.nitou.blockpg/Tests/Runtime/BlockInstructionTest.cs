using NUnit.Framework;
using UnityEngine;
using nitou.BlockPG.Interface;
using nitou.BlockPG.Blocks.Instruction;

namespace RuntimeTests {

    using PrefabName = BlockPGTestEnv.PrefabName;

    /// <summary>
    /// ブロックの機能実装（<see cref="I_BPG_Instruction"/>）の参照を検証する．
    /// </summary>
    /// <remarks>
    /// [NOTE] Facade が Instruction を公開しているにもかかわらず、どこからも代入されておらず
    ///        常に null だった．「実装済みに見えて必ず null」という状態を防ぐためのテスト．
    /// </remarks>
    public class BlockInstructionTest {

        private BlockPGTestEnv _env;

        [SetUp]
        public void SetUp() => _env = new BlockPGTestEnv();

        [TearDown]
        public void TearDown() => _env.Dispose();


        /// <summary>
        /// 検証用の機能実装．
        /// </summary>
        private sealed class TestInstruction : BPG_BlockInstruction { }


        /// ----------------------------------------------------------------------------

        [Test]
        public void 機能実装を持たないブロックではnullになる() {
            var block = _env.CreateBlock(PrefabName.Normal);

            Assert.That(block.Instruction, Is.Null);
            Assert.That(block.HasInstruction(), Is.False);
        }

        [Test]
        public void 付けた機能実装を参照できる() {
            var block = _env.CreateBlock(PrefabName.Normal);
            var instruction = block.RectTransform.gameObject.AddComponent<TestInstruction>();

            Assert.That(block.Instruction, Is.SameAs(instruction),
                "Facade から機能実装を辿れない．");
            Assert.That(block.HasInstruction(), Is.True);
        }

        [Test]
        public void 機能実装から所属ブロックを辿れる() {
            var block = _env.CreateBlock(PrefabName.Normal);
            var instruction = block.RectTransform.gameObject.AddComponent<TestInstruction>();

            Assert.That(instruction.Block, Is.SameAs(block));
        }

        [Test]
        public void 破棄した機能実装は参照されない() {
            // [NOTE] インターフェース型の == は UnityEngine.Object の比較演算子を通らないため、
            //        破棄済みの参照をそのまま返してしまう危険がある．
            var block = _env.CreateBlock(PrefabName.Normal);
            var instruction = block.RectTransform.gameObject.AddComponent<TestInstruction>();
            Assert.That(block.HasInstruction(), Is.True, "前提: 参照できていること");

            Object.DestroyImmediate(instruction);

            Assert.That(block.HasInstruction(), Is.False, "破棄済みのコンポーネントを返している．");
            Assert.That(block.Instruction, Is.Null);
        }

        [Test]
        public void 生成後に付けた機能実装も参照できる() {
            // [NOTE] パレットから生成した後に機能を割り当てる使い方を想定する．
            //        Awake 時点のみで解決すると、この経路で拾えない．
            var block = _env.CreateBlock(PrefabName.Scope);
            Assert.That(block.Instruction, Is.Null, "前提: 最初は持たないこと");

            block.RectTransform.gameObject.AddComponent<TestInstruction>();

            Assert.That(block.HasInstruction(), Is.True);
        }
    }
}
