using UnityEngine;

namespace nitou.BlockPG.Blocks.Section {

    /// <summary>
    /// 文字列を入力する要素．
    /// </summary>
    public sealed class BPG_BlockSectionHeader_TextInput : BPG_BlockSectionHeader_InputFieldBase {

        /// <summary>最大文字数．（※0以下の場合は無制限）</summary>
        [SerializeField] int _maxLength = 0;


        /// ----------------------------------------------------------------------------
        // Protected Method

        protected override string Normalize(string value) {
            value ??= string.Empty;

            return (_maxLength > 0 && value.Length > _maxLength)
                ? value.Substring(0, _maxLength)
                : value;
        }
    }
}
