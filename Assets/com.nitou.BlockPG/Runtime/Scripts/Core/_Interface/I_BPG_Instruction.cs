namespace nitou.BlockPG.Interface {

    /// <summary>
    /// ブロックの機能実装を担う拡張点．
    /// </summary>
    /// <remarks>
    /// [NOTE] 本ライブラリが提供するのは「ブロックを組む UI」までで、組んだものを解釈・実行する
    ///        処理系は持たない．実行の仕組みは利用側が用意し、その入り口としてここを使う．
    ///        そのため実行方法（同期か非同期か、どのスケジューラで回すか）は意図的に規定していない．
    ///        規定すると、利用側の実行モデルを先に縛ることになるため．
    /// </remarks>
    public interface I_BPG_Instruction {

        /// <summary>
        /// 紐づくブロック．
        /// </summary>
        I_BPG_Block Block { get; }
    }
}
