namespace nitou.BlockPG.Blocks {

    /// <summary>
    /// ブロック階層を積み上げる方向．
    /// </summary>
    /// <remarks>
    /// セクションの並び、ヘッダーとボディの並び、子ブロックの並びが
    /// すべてこの方向に従う．
    /// </remarks>
    public enum BlockLayoutAxis {

        /// <summary>上から下へ積む．（※文を並べるブロック）</summary>
        Vertical,

        /// <summary>左から右へ並べる．（※式を組み立てるブロック）</summary>
        Horizontal,
    }


    /// <summary>
    /// <see cref="BlockLayoutAxis"/>の拡張メソッド．
    /// </summary>
    public static class BlockLayoutAxisExtensions {

        public static bool IsVertical(this BlockLayoutAxis self) {
            return self == BlockLayoutAxis.Vertical;
        }

        /// <summary>
        /// 積み上げ方向の成分を取り出す．
        /// </summary>
        public static float Along(this BlockLayoutAxis self, UnityEngine.Vector2 size) {
            return self.IsVertical() ? size.y : size.x;
        }

        /// <summary>
        /// 積み上げ方向と直交する成分を取り出す．
        /// </summary>
        public static float Across(this BlockLayoutAxis self, UnityEngine.Vector2 size) {
            return self.IsVertical() ? size.x : size.y;
        }

        /// <summary>
        /// 積み上げ方向と直交方向の値から、サイズを組み立てる．
        /// </summary>
        public static UnityEngine.Vector2 ToSize(this BlockLayoutAxis self, float along, float across) {
            return self.IsVertical()
                ? new UnityEngine.Vector2(across, along)
                : new UnityEngine.Vector2(along, across);
        }
    }
}
