namespace nitou.BlockPG.Interface {

    /// <summary>
    /// ブロックのレイアウト計算から除外されるオブジェクト．
    /// </summary>
    /// <remarks>
    /// 選択枠・バッジ・エラーアイコンなど、ブロックに重ねて表示する装飾を
    /// 積み上げの対象から外すために使う．
    ///
    /// [NOTE] uGUI の <c>ILayoutIgnorer</c> とは無関係．
    ///        ブロックのレイアウトは LayoutGroup に依存しないため、
    ///        除外の指定もライブラリ側で完結させる．
    ///        （名前を似せると取り違えの原因になるため、接頭辞で区別している）
    ///
    /// 単純に除外したいだけなら <c>BPG_LayoutIgnore</c> を付ければよい．
    /// 既存のコンポーネントに実装させれば、状態に応じて切り替えることもできる．
    /// </remarks>
    public interface I_BPG_LayoutIgnore {

        /// <summary>
        /// レイアウト計算から除外するかどうか．
        /// </summary>
        bool IgnoreLayout { get; }
    }
}
