using System.Linq;
using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;
    using nitou.BlockPG.Events;

    /// <summary>
    /// ブロックの生成と破棄．
    /// </summary>
    /// <remarks>
    /// [NOTE] プレハブの解決（<see cref="I_BPG_BlockCatalog"/>）と実体の生成
    ///        （<see cref="I_BPG_BlockFactory"/>）は環境ごとに差し替えられる．
    ///        ここに残すのは、生成後の整理・環境への接続・イベント発行という
    ///        ライブラリ側の決めごとだけ．
    /// </remarks>
    public static class BPG_BlockUtils {

        // ※環境を渡さない経路のための既定．Resources から引く．
        private static readonly BPG_ResourcesBlockCatalog DefaultCatalog = new();


        /// ----------------------------------------------------------------------------
        // プレハブの解決

        /// <summary>
        /// ブロック名からプレハブを取得する．（※見つからない場合はnull）
        /// </summary>
        public static I_BPG_Block LoadBlockPrefab(string prefabName, I_BPG_ProgrammingEnv programmingEnv) {
            var catalog = (programmingEnv != null && programmingEnv.BlockCatalog != null)
                ? programmingEnv.BlockCatalog
                : DefaultCatalog;

            return catalog.GetPrefab(prefabName);
        }

        /// <summary>
        /// ブロック名からプレハブを取得する．（※Resources から引く）
        /// </summary>
        public static TBlock LoadBlockPrefab<TBlock>(string prefabName)
            where TBlock : BPG_BlockBase {

            return DefaultCatalog.GetPrefab(prefabName) as TBlock;
        }

        /// <summary>
        /// ブロック名からプレハブを取得する．（※Resources から引く）
        /// </summary>
        public static BPG_BlockBase LoadBlockPrefab(string prefabName) {
            return LoadBlockPrefab<BPG_BlockBase>(prefabName);
        }


        /// ----------------------------------------------------------------------------
        // 生成

        /// <summary>
        /// プレハブからブロックを生成し、環境へ配置する．
        /// </summary>
        public static I_BPG_Block CreateBlock(I_BPG_Block blockPrefab, I_BPG_ProgrammingEnv programmingEnv) {
            if (blockPrefab == null)
                throw new System.ArgumentNullException(nameof(blockPrefab));
            if (programmingEnv == null)
                throw new System.ArgumentNullException(nameof(programmingEnv));

#if UNITY_EDITOR
            // ※撤去し損ねた LayoutGroup があると配置が二重に効く（プレハブごとに1回だけ通知）
            BPG_LayoutGroupGuard.WarnOnceForPrefab(blockPrefab.RectTransform.gameObject);
#endif

            var factory = programmingEnv.BlockFactory;
            if (factory == null) {
                Debug.LogWarning("Block factory is not available.");
                return null;
            }

            // ※配置先を指定して生成させる（後から付け替えると Canvas のスケールが混入する）
            var block = factory.Create(blockPrefab, programmingEnv.RectTransform);
            if (block == null) {
                Debug.LogWarning($"Failed to create block. (prefab: {blockPrefab.RectTransform.name})");
                return null;
            }

            // setup param
            // [NOTE] 名前は復元時にプレハブを引く鍵になるため、プレハブ名をそのまま付ける．
            block.RectTransform.name = blockPrefab.RectTransform.name;
            block.RectTransform.localPosition = Vector3.zero;
            block.RectTransform.localEulerAngles = Vector3.zero;
            block.RectTransform.localScale = Vector3.one;

            programmingEnv.Append(block);

            BPG_BlockEventBus.PublishCreateEvent(block);
            return block;
        }

        /// <summary>
        /// プレハブからブロックを生成し、環境へ配置する．
        /// </summary>
        public static TBlock CreateBlock<TBlock>(TBlock blockPrefab, I_BPG_ProgrammingEnv programmingEnv)
            where TBlock : BPG_BlockBase {

            var block = CreateBlock((I_BPG_Block)blockPrefab, programmingEnv);
            if (block == null)
                return null;

            // ※差し替えたファクトリが別の型を返した場合は、呼び出し側の期待と食い違う
            if (block is TBlock typed)
                return typed;

            Debug.LogWarning($"Created block is not {typeof(TBlock).Name}. " +
                $"(prefab: {blockPrefab.name}, actual: {block.GetType().Name})");
            return null;
        }


        /// ----------------------------------------------------------------------------
        // 破棄

        /// <summary>
        /// ブロックを子孫ごと破棄する．
        /// </summary>
        public static void RemoveBlock(I_BPG_Block block) {
            if (block is null)
                return;

            // 子のブロックから逆順で破棄イベントを発火
            foreach (var childBlock in block.GetAllChildBlocks(containSelf: true).Reverse<I_BPG_Block>()) {
                BPG_BlockEventBus.PublishDestroyEvent(childBlock);
            }

            // 破棄
            Object.Destroy(block.RectTransform.gameObject);
        }
    }
}
