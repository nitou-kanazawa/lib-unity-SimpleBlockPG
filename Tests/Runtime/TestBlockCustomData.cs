using UnityEngine;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    /// <summary>
    /// 検証用のブロック固有データ．
    /// </summary>
    /// <remarks>
    /// [NOTE] 復元はプレハブからの生成で行われるため、実行時に付けたコンポーネントは
    ///        復元後のブロックには存在しない．保存と復元を通しで検証するには、
    ///        受け手がプレハブ側に居る必要がある．
    ///        そのため検証用プレハブ <c>Block [TestInput]</c> にこれを付けてある．
    /// </remarks>
    public sealed class TestBlockCustomData : MonoBehaviour, I_BPG_BlockCustomData {

        [SerializeField] string _data = string.Empty;

        public string Data {
            get => _data;
            set => _data = value ?? string.Empty;
        }

        public string SaveCustomData() => _data;

        public void LoadCustomData(string data) => _data = data ?? string.Empty;
    }
}
