using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace nitou.Utils {

    /// <summary>
    /// <see cref="Transform"/>の拡張メソッド．
    /// </summary>
    /// <remarks>
    /// [NOTE] 以前はグローバル名前空間に置かれており、ライブラリを取り込んだ側の
    ///        名前空間を汚染していた．
    /// </remarks>
    public static class TransformExtensions {

        /// <summary>
        /// 直下の子を列挙する．
        /// </summary>
        public static IEnumerable<Transform> GetChildren(this Transform self) {
            return self.Cast<Transform>();
        }
    }
}
