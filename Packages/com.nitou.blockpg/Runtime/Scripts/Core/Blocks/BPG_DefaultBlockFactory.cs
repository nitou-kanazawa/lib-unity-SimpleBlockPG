using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// <c>Object.Instantiate</c>で実体を作る既定のファクトリ．
    /// </summary>
    public sealed class BPG_DefaultBlockFactory : I_BPG_BlockFactory {

        /// <summary>
        /// プレハブから実体を作る．（※失敗した場合はnull）
        /// </summary>
        /// <remarks>
        /// [NOTE] 配置先を指定して生成する．シーンルートに生成してから付け替えると、
        ///        Canvas のスケールを打ち消す localScale が入ってしまう．
        /// </remarks>
        public I_BPG_Block Create(I_BPG_Block prefab, RectTransform parent) {
            if (prefab == null)
                return null;

            var instance = Object.Instantiate(prefab.RectTransform.gameObject, parent);
            if (!instance.TryGetComponent<I_BPG_Block>(out var block)) {
                Debug.LogWarning($"Created object does not have a block component. (name: {instance.name})", instance);
                Object.Destroy(instance);
                return null;
            }
            return block;
        }
    }
}
