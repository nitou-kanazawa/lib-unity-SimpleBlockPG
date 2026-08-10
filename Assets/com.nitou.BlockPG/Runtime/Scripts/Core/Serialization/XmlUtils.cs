using UnityEngine;

namespace nitou.BlockPG.Serialization {

    /// <summary>
    /// XML変換に関連した汎用メソッド集．
    /// </summary>
    public class XmlUtils {
        // [TODO]
        //  基本型と文字列の変換はちゃんと調べて整備したい（※現状優先度は低い）

        public static Vector3 StringToVector3(string stringValue) {

            // 例外処理を追加し、入力がnullまたは空の場合には Vector3.zero を返す
            if (string.IsNullOrWhiteSpace(stringValue)) {
                Debug.LogWarning("Input string is null or empty. Returning Vector3.zero.");
                return Vector3.zero;
            }

            // 不要な空白や括弧を削除し、カンマで分割
            string[] xyz = stringValue.TrimStart('(').TrimEnd(')').Split(',');

            // xyz 配列の長さをチェックし、期待する3要素がなければ Vector3.zero を返す
            if (xyz.Length != 3) {
                Debug.LogWarning($"Input string \"{stringValue}\" does not contain 3 elements. Returning Vector3.zero.");
                return Vector3.zero;
            }

            // 各値を安全に float に変換。変換できない場合も Vector3.zero を返す
            if (float.TryParse(xyz[0].Trim(), out float x) &&
                float.TryParse(xyz[1].Trim(), out float y) &&
                float.TryParse(xyz[2].Trim(), out float z)) {
                return new Vector3(x, y, z);
            } else {
                Debug.LogWarning($"Input string \"{stringValue}\" contains invalid float values. Returning Vector3.zero.");
                return Vector3.zero;
            }
        }

    }

}