using UnityEngine;

namespace nitou.BlockPG.Interface{

    public interface I_BPG_Spot {

        RectTransform RectTransform { get; }

        Vector2 DropPosition { get; }

        I_BPG_Block Block { get; }
    }


    /// <summary>
    /// <see cref="I_BPG_Spot"/>型の汎用的な拡張メソッド集．
    /// </summary>
    /// <remarks>
    /// [NOTE] スポットの種類ごとの接続処理は nitou.BlockPG.DragDrop 側に
    ///        <c>BPG_SpotConnect_Extensions</c> として置いている．
    ///        以前は両方が同名だったため、どちらの拡張メソッドなのか追いづらかった．
    /// </remarks>
    public static class BPG_Spot_Extensions {

        /// <summary>
        /// スポットが所属する<see cref="I_BPG_ProgrammingEnv"/>を取得する．
        /// </summary>
        public static I_BPG_ProgrammingEnv GetBelongedProgEnv(this I_BPG_Spot self) {
            
            // get from self or parent
            var programmingEnv = self.RectTransform.GetComponentInParent<I_BPG_ProgrammingEnv>();
            
            // Get from child
            if (programmingEnv == null && self.RectTransform.childCount > 0) {
                programmingEnv = self.RectTransform.GetChild(0).GetComponent<I_BPG_ProgrammingEnv>();
            }

            return programmingEnv;
        }

    }
}
