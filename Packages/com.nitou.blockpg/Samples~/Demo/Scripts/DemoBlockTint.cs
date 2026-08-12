using UnityEngine;
using nitou.BlockPG.Interface;

namespace nitou.BlockPG.Demo {

    /// <summary>
    /// ブロックごとの色替え．ブロック固有データとして保存される．
    /// </summary>
    /// <remarks>
    /// [NOTE] 入力値では表せない情報の例として置いている．
    ///        <see cref="I_BPG_BlockCustomData"/>を実装したコンポーネントをブロックと同じ
    ///        GameObject に付けると、保存データへ一緒に書き出される．
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class DemoBlockTint : MonoBehaviour, I_BPG_BlockCustomData {

        /// <summary>色替えの選択肢．（※-1は「テーマ既定の色」）</summary>
        public static readonly Color[] Palette = {
            new Color(0.95f, 0.35f, 0.42f),
            new Color(0.98f, 0.72f, 0.22f),
            new Color(0.33f, 0.78f, 0.52f),
            new Color(0.35f, 0.60f, 0.95f),
            new Color(0.70f, 0.48f, 0.94f),
        };

        private int _index = -1;

        /// <summary>
        /// 選択中の色．（※未設定の場合は-1）
        /// </summary>
        public int Index => _index;

        /// <summary>
        /// 色が設定されているかどうか．
        /// </summary>
        public bool HasTint => 0 <= _index && _index < Palette.Length;

        /// <summary>
        /// 選択中の色．（※未設定の場合は既定値）
        /// </summary>
        public Color Color => HasTint ? Palette[_index] : Color.white;


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// 次の色へ切り替える．（※一周すると未設定へ戻る）
        /// </summary>
        public void Next() {
            _index = (_index + 1 >= Palette.Length) ? -1 : _index + 1;
        }


        /// ----------------------------------------------------------------------------
        // I_BPG_BlockCustomData

        // [NOTE] 未設定なら空を返す．空文字を返した場合は保存データに要素ごと現れない．
        public string SaveCustomData() {
            return HasTint ? _index.ToString() : string.Empty;
        }

        public void LoadCustomData(string data) {
            _index = int.TryParse(data, out var index) ? index : -1;
        }
    }
}
