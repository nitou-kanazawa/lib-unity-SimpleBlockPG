using UnityEngine;

namespace nitou.BlockPG.Blocks.Section {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// <see cref="BPG_BlockSectionHeader"/>直下に配置されるレイアウト要素．
    /// </summary>
    public sealed class BPG_BlockSectionHeader_Item : BPG_ComponentBase, 
        I_BPG_BlockSectionHeaderItem {

        /// <summary>
        /// サイズ情報．
        /// </summary>
        public Vector2 Size => RectTransform.sizeDelta;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        /// <summary>
        /// 自身のサイズ変化を検知する．（ラベルの文字数変更など）
        /// [NOTE] アイテムのサイズはヘッダー幅の算出根拠になるため、ブロックの更新を予約する．
        ///        ヘッダー自身のサイズはレイアウト処理側が設定するため、
        ///        ヘッダーで同じ検知を行うと更新が無限に予約され続ける点に注意．
        /// </summary>
        private void OnRectTransformDimensionsChange() {
            var parent = transform.parent;
            if (parent != null) {
                parent.GetComponentInParent<I_BPG_BlockLayout>()?.SetLayoutDirty();
            }
        }
    }
}
