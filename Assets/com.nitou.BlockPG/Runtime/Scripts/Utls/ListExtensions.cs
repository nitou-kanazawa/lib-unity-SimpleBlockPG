using System.Collections.Generic;

namespace nitou.Utils {

    /// <summary>
    /// Extension methods for type of <see cref="IList{T}"/>.
    /// </summary>
    public static class ListExtensions {

        /// <summary>
        /// 指定したインデックスがリスト範囲内か確認する
        /// </summary>
        public static bool IsInRange<T>(this int index, IReadOnlyList<T> list) {
            return 0 <= index && index < list.Count;
        }

        /// <summary>
        /// 指定したインデックスがリスト範囲外か確認する
        /// </summary>
        public static bool IsOutOfRange<T>(this int index, IReadOnlyList<T> list) {
            return !index.IsInRange(list);
        }

        /// <summary>
        /// 指定された要素のインデックスを取得する．
        /// 見つからない場合は -1 を返す．
        /// </summary>
        public static int IndexOf<T>(this IReadOnlyList<T> list, T item) {
            for (int i = 0; i < list.Count; i++) {
                if (EqualityComparer<T>.Default.Equals(list[i], item)) {
                    return i;
                }
            }
            return -1; // 見つからない場合
        }
    }
}
