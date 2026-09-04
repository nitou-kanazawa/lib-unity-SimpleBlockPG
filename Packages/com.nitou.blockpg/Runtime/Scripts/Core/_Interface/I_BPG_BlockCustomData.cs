namespace nitou.BlockPG.Interface {

    /// <summary>
    /// ブロック固有のデータを保存・復元するための拡張点．
    /// </summary>
    /// <remarks>
    /// ブロックと同じ<c>GameObject</c>に付けると、保存データへ一緒に書き出される．
    /// ヘッダーの入力値では表せない情報（参照先のID、色、独自の設定など）を持たせる用途．
    ///
    /// [NOTE] 文字列で受け渡すのは、保存形式を XML に閉じ込めるため．
    ///        中身の形式（JSON でも独自形式でも）は実装側が決めてよい．
    /// </remarks>
    public interface I_BPG_BlockCustomData {

        /// <summary>
        /// 保存する文字列を返す．（※保存するものが無い場合はnullまたは空）
        /// </summary>
        string SaveCustomData();

        /// <summary>
        /// 保存された文字列から復元する．
        /// </summary>
        void LoadCustomData(string data);
    }
}
