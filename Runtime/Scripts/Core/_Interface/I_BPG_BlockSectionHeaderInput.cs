using System;

namespace nitou.BlockPG.Interface {

    /// <summary>
    /// ヘッダーに置かれる入力要素のインターフェース．
    /// </summary>
    /// <remarks>
    /// 値を文字列で扱うのは、保存形式（<c>SerializableInput</c>）が文字列 1 本のため．
    /// 数値や選択肢も、それぞれの型で解釈したうえで文字列として持つ．
    ///
    /// [NOTE] 具体的な UI ウィジェットには依存しない．入力欄の実体（TMP_InputField など）との
    ///        橋渡しは各実装クラスが行う．
    /// </remarks>
    public interface I_BPG_BlockSectionHeaderInput : I_BPG_BlockSectionHeaderItem {

        /// <summary>
        /// 現在の入力値．
        /// </summary>
        string Value { get; }

        /// <summary>
        /// 入力値を設定する．（※セーブデータからの復元時にも使用する）
        /// </summary>
        void SetValue(string value);

        /// <summary>
        /// 入力値が変化した時に通知する．
        /// </summary>
        event Action<I_BPG_BlockSectionHeaderInput> OnValueChanged;
    }
}
