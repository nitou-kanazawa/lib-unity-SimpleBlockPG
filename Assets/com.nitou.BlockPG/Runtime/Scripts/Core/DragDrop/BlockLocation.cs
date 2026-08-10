using UnityEngine;

namespace nitou.BlockPG.DragDrop{
    using Location = BlockLocation;

    /// <summary>
	/// ブロック配置場所の分類.
	/// </summary>
	public enum BlockLocation {

        /// <summary>ドラッグソース領域</summary>
        Outside,

        /// <summary>ProgrammingEnvのフリー領域</summary>
        ProgEnv,

        /// <summary>ProgrammingEnvのBlockStackr領域</summary>
        Stack,

        /// <summary>※確認中</summary>
        InputSpot,
	}

    /// <summary>
    /// ドラッグ処理の結果
    /// </summary>
	public enum DraggingResult {

		// ブロックを生成した
		CreateBlock,

		// ブロックを破棄した
		DestroyBlock,

		// ブロックを移動された
		Move,

		// ブロックを移動した (※接続関係に変化なし)
		FreeMove,
	}


	/// <summary>
	/// <see cref="I_BE2_Block"/>のドラッグ操作に関する汎用メソッド集
	/// </summary>
	public static class DraggingUtil {

		/// <summary>
		/// ドラッグ操作による結果を判定する
		/// </summary>
		public static DraggingResult CheckResult(Location from, Location to) {
			return (from, to) switch {
				// ブロックの生成 (※Outsideからブロックのドラッグが開始された場合)
				(Location.Outside, Location.ProgEnv) => DraggingResult.CreateBlock,
				(Location.Outside, Location.Stack) => DraggingResult.CreateBlock,
				(Location.Outside, Location.InputSpot) => DraggingResult.CreateBlock,

				// ブロックの削除 (※Outsideにブロックをドロップする場合)
				(Location.ProgEnv, Location.Outside) => DraggingResult.DestroyBlock,
				(Location.Stack, Location.Outside) => DraggingResult.DestroyBlock,
				(Location.InputSpot, Location.Outside) => DraggingResult.DestroyBlock,

				// ブロックの移動 (※接続関係の変化なし)
				(Location.ProgEnv, Location.ProgEnv) => DraggingResult.FreeMove,

				// ブロックの移動
				_ => DraggingResult.Move
			};
		}

	}

}
