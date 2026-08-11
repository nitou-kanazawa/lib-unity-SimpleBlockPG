using System.Collections.Generic;
using UnityEngine;

namespace nitou.BlockPG.Interface {

    public interface I_BPG_ProgrammingEnv {

        RectTransform RectTransform { get; }

        /// <summary>
        /// ブロック名からプレハブを引くカタログ．
        /// </summary>
        /// <remarks>
        /// [NOTE] 静的なグローバルではなく環境に持たせている．生成と復元の経路には
        ///        いずれも環境が渡ってくるため、差し替えの単位を環境に揃えられる．
        ///        （テストごとに使い捨ての環境を作れば、差し替えも自然に分離される）
        /// </remarks>
        I_BPG_BlockCatalog BlockCatalog { get; }

        /// <summary>
        /// プレハブからブロックの実体を作るファクトリ．
        /// </summary>
        I_BPG_BlockFactory BlockFactory { get; }
    }


    /// <summary>
    /// Extension methods for type of <see cref="I_BPG_ProgrammingEnv"/>.
    /// </summary>
    public static class BPG_ProgrammingEnv_Extensions {

        /// ----------------------------------------------------------------------------
        // 接続

        public static void Append(this I_BPG_ProgrammingEnv self, I_BPG_Block block) {
            block.RectTransform.SetParent(self.RectTransform);
            block.SetParentSection(null);
        }

        /// <summary>
        ///
        /// </summary>
        public static void Append(this I_BPG_ProgrammingEnv self, I_BPG_Draggable draggabble) {

            draggabble.RectTransform.SetParent(self.RectTransform);
            draggabble.Block.SetParentSection(null);
        }


        /// ----------------------------------------------------------------------------
        // 走査と削除

        /// <summary>
        /// 環境直下のルートブロックを取得する．
        /// </summary>
        /// <remarks>
        /// [NOTE] Destroy はフレーム終端まで遅延するため、同じフレーム内では
        ///        破棄予定のブロックも子として残っている．非アクティブ化を目印に除外する．
        /// </remarks>
        public static IReadOnlyList<I_BPG_Block> GetRootBlocks(this I_BPG_ProgrammingEnv self) {
            var blocks = new List<I_BPG_Block>();
            foreach (Transform child in self.RectTransform) {
                if (child.gameObject.activeSelf && child.TryGetComponent<I_BPG_Block>(out var block)) {
                    blocks.Add(block);
                }
            }
            return blocks;
        }

        /// <summary>
        /// 識別IDからブロックを探す．（※見つからない場合はnull）
        /// </summary>
        /// <remarks>
        /// 復元を行うとインスタンスが作り直されるため、選択状態などを
        /// 参照ではなくIDで保持しておき、ここで引き直す．
        /// </remarks>
        public static I_BPG_Block FindBlockById(this I_BPG_ProgrammingEnv self, string id) {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (var root in self.GetRootBlocks()) {
                foreach (var block in root.GetAllChildBlocks(containSelf: true)) {
                    if (block.Id == id) {
                        return block;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 環境内のブロックをすべて取り除く．
        /// </summary>
        /// <remarks>
        /// [NOTE] ブロックの破棄イベントは発行しない．
        ///        復元にともなう作り直しを、利用者による削除と区別するため．
        /// </remarks>
        public static void RemoveAllBlocks(this I_BPG_ProgrammingEnv self) {
            foreach (var block in self.GetRootBlocks()) {
                var obj = block.RectTransform.gameObject;

                // ※Destroy は遅延するため、同フレームの走査から外れるよう先に無効化する
                obj.SetActive(false);
                Object.Destroy(obj);
            }
        }
    }

}
