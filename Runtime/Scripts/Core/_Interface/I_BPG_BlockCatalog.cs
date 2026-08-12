using UnityEngine;

namespace nitou.BlockPG.Interface {

    /// <summary>
    /// ブロック名からプレハブを引く．
    /// </summary>
    /// <remarks>
    /// 既定では <c>Resources/BlockPG/</c> から読む．Addressables や独自の管理へ
    /// 差し替えたい場合、またはテストでダミーを注入したい場合にこれを実装する．
    ///
    /// [NOTE] 保存データはブロック名だけを持つため、名前からプレハブを引く経路が
    ///        復元の要になる．ここが固定されていると、実資産なしでは復元を検証できない．
    /// </remarks>
    public interface I_BPG_BlockCatalog {

        /// <summary>
        /// ブロック名に対応するプレハブを取得する．（※見つからない場合はnull）
        /// </summary>
        I_BPG_Block GetPrefab(string blockName);
    }


    /// <summary>
    /// プレハブからブロックの実体を作る．
    /// </summary>
    /// <remarks>
    /// 既定では <c>Object.Instantiate</c> をそのまま呼ぶ．
    /// プールを挟みたい場合にこれを実装する．
    ///
    /// [NOTE] 生成後の名前・座標の整理、環境への接続、生成イベントの発行は
    ///        ライブラリ側の責務として <c>BPG_BlockUtils</c> に残す．
    ///        ここで担うのは「実体をどう用意するか」だけ．
    /// </remarks>
    public interface I_BPG_BlockFactory {

        /// <summary>
        /// プレハブから実体を作る．（※失敗した場合はnull）
        /// </summary>
        I_BPG_Block Create(I_BPG_Block prefab, RectTransform parent);
    }
}
