using System.Globalization;
using UnityEngine;

namespace nitou.BlockPG.Serialization {

    /// <summary>
    /// XML変換に関連した汎用メソッド集．
    /// </summary>
    public class XmlUtils {
        // [NOTE]
        //  セーブデータは実行環境をまたいで読み書きされるため、
        //  文字列との相互変換では必ず InvariantCulture を指定すること．
        //  （小数点にカンマを使うロケールでは、区切り文字のカンマと衝突して壊れる）

        /// <summary>
        /// <see cref="Vector3"/>をXML保存用の文字列へ変換する．
        /// </summary>
        public static string Vector3ToString(Vector3 value) {
            // [NOTE] Vector3.ToString() はカルチャ依存かつ小数点以下2桁に丸められるため使用しない．
            return string.Format(CultureInfo.InvariantCulture, "({0:R}, {1:R}, {2:R})", value.x, value.y, value.z);
        }

        /// <summary>
        /// XML保存用の文字列を<see cref="Vector3"/>へ変換する．
        /// </summary>
        public static Vector3 StringToVector3(string stringValue) {

            // 例外処理を追加し、入力がnullまたは空の場合には Vector3.zero を返す
            if (string.IsNullOrWhiteSpace(stringValue)) {
                Debug.LogWarning("Input string is null or empty. Returning Vector3.zero.");
                return Vector3.zero;
            }

            // 不要な空白や括弧を削除し、カンマで分割
            string[] xyz = stringValue.Trim().TrimStart('(').TrimEnd(')').Split(',');

            // xyz 配列の長さをチェックし、期待する3要素がなければ Vector3.zero を返す
            if (xyz.Length != 3) {
                Debug.LogWarning($"Input string \"{stringValue}\" does not contain 3 elements. Returning Vector3.zero.");
                return Vector3.zero;
            }

            // 各値を安全に float に変換。変換できない場合も Vector3.zero を返す
            if (TryParseFloat(xyz[0], out float x) &&
                TryParseFloat(xyz[1], out float y) &&
                TryParseFloat(xyz[2], out float z)) {
                return new Vector3(x, y, z);
            } else {
                Debug.LogWarning($"Input string \"{stringValue}\" contains invalid float values. Returning Vector3.zero.");
                return Vector3.zero;
            }
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private static bool TryParseFloat(string value, out float result) {
            return float.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }
    }

}
