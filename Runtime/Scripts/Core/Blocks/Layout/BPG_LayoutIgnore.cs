using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// このオブジェクトをブロックのレイアウト計算から除外する．
    /// </summary>
    /// <remarks>
    /// ブロックに重ねて表示する装飾（選択枠・バッジ・エラーアイコンなど）に付ける．
    /// 付いていないオブジェクトは、直下の子であれば積み上げの対象になる．
    ///
    /// 状態に応じて切り替えたい場合は、独自のコンポーネントに
    /// <see cref="I_BPG_LayoutIgnore"/> を実装すればよい．
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("BlockPG/Layout Ignore")]
    public sealed class BPG_LayoutIgnore : MonoBehaviour, I_BPG_LayoutIgnore {

        /// <summary>
        /// レイアウト計算から除外するかどうか．
        /// </summary>
        public bool IgnoreLayout => true;
    }
}
