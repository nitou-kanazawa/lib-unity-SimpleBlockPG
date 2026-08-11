using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using nitou.Utils;

namespace nitou.BlockPG.Interface {

    // [NOTE]
    //  親階層の情報 : ParentSection
    //  自階層の情報 : Drag, Instruction, Layout
    //  子階層の情報 : Layout

    /// <summary>
    /// ブロックインスタンスのインターフェース．Facadeとして機能する．
    /// </summary>
    public interface I_BPG_Block {

        /// <summary>
        /// ブロックの位置とサイズを制御するRectTransform．
        /// </summary>
        RectTransform RectTransform { get; }

        /// <summary>
        /// 識別ID．インスタンスごとに一意で、実行をまたいでも保存・復元できる．
        /// </summary>
        string Id { get; }

        /// <summary>
        /// ブロック分類．
        /// </summary>
        BlockType Type { get; }

        /// <summary>
        /// 親セクション．ルートブロックの場合はnullになる．
        /// </summary>
        I_BPG_BlockSection ParentSection { get; }

        /// <summary>
        /// 子階層のレイアウト．
        /// </summary>
        I_BPG_BlockLayout Layout { get; }

        /// <summary>
        /// ドラッグ操作時の挙動．
        /// </summary>
        I_BPG_Draggable Drag { get; }

        /// <summary>
        /// ブロックの機能実装．（※持たない場合はnull）
        /// </summary>
        /// <remarks>
        /// [NOTE] 実行の処理系はライブラリ側では提供しない．
        ///        利用側が <see cref="Blocks.Instruction.BPG_BlockInstruction"/> を継承して付ける．
        /// </remarks>
        I_BPG_Instruction Instruction { get; }

        /// <summary>
        /// 親セクションを設定する．
        /// </summary>
        void SetParentSection(I_BPG_BlockSection parentSection);

        /// <summary>
        /// 識別IDを設定する．（※セーブデータからの復元時に使用する）
        /// </summary>
        void SetId(string id);
    }


    /// <summary>
    /// <see cref="I_BPG_Block"/>型の汎用的な拡張メソッド集．
    /// </summary>
    public static class BPG_Block_Extensions {

        /// ----------------------------------------------------------------------------
        #region Info

        public static bool IsTrigger(this I_BPG_Block self) {
            return self.Type is BlockType.Trigger;
        }

        public static bool IsCondition(this I_BPG_Block self) {
            return self.Type is BlockType.Condition;
        }

        public static int GetGameObjectID(this I_BPG_Block self) {
            return self.RectTransform.gameObject.GetInstanceID();
        }

        /// <summary>
        /// 親ブロックが存在するか判定する．
        /// </summary>
        public static bool HasParentBlock(this I_BPG_Block self) {
            return self.GetParentBlock() != null;
        }

        /// <summary>
        /// 機能実装が付いているか判定する．
        /// </summary>
        /// <remarks>
        /// [NOTE] <see cref="HasLayout"/>と同じ理由で、破棄済みのコンポーネントを弾くために
        ///        UnityEngine.Object として比較する．
        /// </remarks>
        public static bool HasInstruction(this I_BPG_Block self) {
            return self.Instruction is UnityEngine.Object obj
                ? obj != null
                : self.Instruction != null;
        }

        /// <summary>
        /// ブロック固有のデータを取得する．（※持たない場合はnull）
        /// </summary>
        public static string GetCustomData(this I_BPG_Block self) {
            var holder = self.RectTransform.GetComponent<I_BPG_BlockCustomData>();
            return (holder != null) ? holder.SaveCustomData() : null;
        }

        /// <summary>
        /// ブロック固有のデータを復元する．
        /// </summary>
        /// <remarks>
        /// [NOTE] 保存データに固有データがあるのに受け手が居ない場合は、
        ///        黙って捨てず警告する．プレハブ側の構成変更で失われる事故を見つけるため．
        /// </remarks>
        public static void SetCustomData(this I_BPG_Block self, string data) {
            if (string.IsNullOrEmpty(data))
                return;

            var holder = self.RectTransform.GetComponent<I_BPG_BlockCustomData>();
            if (holder == null) {
                Debug.LogWarning($"Custom data is saved but no receiver is attached. " +
                    $"(block: {self.RectTransform.name})", self.RectTransform);
                return;
            }
            holder.LoadCustomData(data);
        }

        /// <summary>
        /// ルートブロックかどうか判定する．
        /// </summary>
        public static bool IsRootBlock(this I_BPG_Block self) {
            return self.ParentSection == null;
        }

        /// <summary>
        /// 親セクション内の最初のブロックかどうか判定する．
        /// 親セクションを持たない場合はfalseを返す．
        /// </summary>
        public static bool IsFirstBlockInSection(this I_BPG_Block self) {
            var sectionBody = self.GetParentSectionBody();
            if (sectionBody == null) return false;

            // ※空リストの場合は null が返る
            return sectionBody.ChildBlocks.FirstOrDefault() == self;
        }

        /// <summary>
        /// 親セクション内の最後のブロックかどうか判定する．
        /// 親セクションを持たない場合はfalseを返す．
        /// </summary>
        public static bool IsLastBlockInSection(this I_BPG_Block self) {
            var sectionBody = self.GetParentSectionBody();
            if (sectionBody == null) return false;

            // ※空リストの場合は null が返る
            return sectionBody.ChildBlocks.LastOrDefault() == self;
        }

        /// <summary>
        /// 親セクション内でのインデックスを取得する．
        /// 親セクションを持たない場合は-1を返す．
        /// </summary>
        public static int GetIndexInSection(this I_BPG_Block self) {
            var sectionBody = self.GetParentSectionBody();
            if (sectionBody == null) return -1;

            return sectionBody.ChildBlocks.IndexOf(self);
        }

        /// <summary>
        /// 所属するセクションのボディを取得する．（※取得できない場合はnull）
        /// </summary>
        /// <remarks>
        /// [NOTE] 子ブロックのリストは構成変化の検知でしか更新されないため、
        ///        実際の子の数と食い違う瞬間がありうる．食い違いは呼び出し側では
        ///        直せないため、原因を追えるよう警告として通知する．
        /// </remarks>
        private static I_BPG_BlockSectionBody GetParentSectionBody(this I_BPG_Block self) {
            var sectionBody = self.ParentSection?.Body;
            if (sectionBody == null)
                return null;

            int listed = sectionBody.ChildBlocks.Count;
            int actual = sectionBody.RectTransform.childCount;
            if (listed != actual) {
                Debug.LogWarning($"Child block list is out of sync. " +
                    $"(section: {sectionBody.RectTransform.name}, listed: {listed}, actual: {actual})",
                    sectionBody.RectTransform);
            }
            return sectionBody;
        }

        #endregion


        /// ----------------------------------------------------------------------------
        #region Getter

        /// <summary>
        /// 所属する親ブロックへの参照を取得する．
        /// </summary>
        public static I_BPG_Block GetParentBlock(this I_BPG_Block self) {
            return (self.ParentSection != null) ? self.ParentSection.Block : null;
        }

        /// <summary>
        /// ルートブロックを取得する．
        /// </summary>
        public static I_BPG_Block GetRootBlock(this I_BPG_Block self) {
            var parentBlock = self.GetParentBlock();
            return (parentBlock != null) ? parentBlock.GetRootBlock() : self;
        }

        /// <summary>
        /// 同じ親を持つ1つ前のブロックを取得する．（※存在しない場合はnull）
        /// </summary>
        public static I_BPG_Block GetPreviousBlock(this I_BPG_Block self) {
            var sectionBody = self.GetParentSectionBody();
            if (sectionBody == null) return null;

            var index = sectionBody.ChildBlocks.IndexOf(self);

            // インデックスが有効範囲内であれば前の要素を返す
            return index > 0 ? sectionBody.ChildBlocks[index - 1] : null;
        }

        /// <summary>
        /// 同じ親を持つ1つ後ろのブロックを取得する．（※存在しない場合はnull）
        /// </summary>
        public static I_BPG_Block GetNextBlock(this I_BPG_Block self) {
            var sectionBody = self.GetParentSectionBody();
            if (sectionBody == null) return null;

            var index = sectionBody.ChildBlocks.IndexOf(self);

            // インデックスが有効範囲内であれば次の要素を返す
            return (0 <= index && index < sectionBody.ChildBlocks.Count - 1)
                ? sectionBody.ChildBlocks[index + 1] : null;
        }



        /// <summary>
        /// 子階層以下の<see cref="I_BE2_Block"/>を再帰的に取得する．
        /// </summary>
        public static List<I_BPG_Block> GetAllChildBlocks(this I_BPG_Block self, bool containSelf = true) {
            if (self == null) return null;

            var blockList = new List<I_BPG_Block>();
            if (containSelf) {
                blockList.Add(self);
            }

            // ※Layoutを持たないブロックは子階層を持てない
            if (!self.HasLayout()) {
                return blockList;
            }

            // 子階層以下
            var childBlocks = self.Layout.Sections
                .Where(section => section != null)
                .SelectMany(section => section.GetAllChildBlocks());
            blockList.AddRange(childBlocks);
            return blockList;
        }

        /// <summary>
        /// 子階層以下の<see cref="I_BPG_Block"/>の数を取得する．
        /// </summary>
        public static int GetAllChildBlocksCount(this I_BPG_Block self, bool containSelf = true) {
            if (self == null)
                throw new System.ArgumentNullException(nameof(self));

            // ※自分自信を含めるかどうか
            int additional = (containSelf ? 1 : 0);

            // ※Layoutを持たないブロックは子階層を持てない
            if (!self.HasLayout()) {
                return additional;
            }

            return additional +
                self.Layout.Sections
                .Where(section => section != null)
                .Select(section => section.GetAllChildBlocksCount())
                .Sum();
        }

        /// <summary>
        /// Obtains a reference to the parent section to which it belongs.
        /// </summary>
        public static I_BPG_BlockSection GetFirstSection(this I_BPG_Block self) {
            return self.HasLayout() ? self.Layout.Sections.FirstOrDefault() : null;
        }

        /// <summary>
        /// 子階層のレイアウトが利用可能かどうか判定する．
        /// </summary>
        /// <remarks>
        /// [NOTE] インターフェース型の == は UnityEngine.Object の比較演算子を通らないため、
        ///        破棄済みのコンポーネントを null と判定できない．
        ///        Object へキャストして Unity の判定に載せる．
        /// </remarks>
        private static bool HasLayout(this I_BPG_Block self) {
            return self.Layout is UnityEngine.Object obj
                ? obj != null
                : self.Layout != null;
        }

        /// <summary>
        /// １つ目のセクション直下のブロックを取得する．
        /// </summary>
        public static IEnumerable<I_BPG_Block> GetFirstSectionBlocks(this I_BPG_Block self) {
            var firstSection = self.GetFirstSection();
            if (firstSection is null)
                return Enumerable.Empty<I_BPG_Block>();

            return firstSection.GetBodyBlocks();
        }

        #endregion


        /// ----------------------------------------------------------------------------
        #region Setter

        /// <summary>
        /// 
        /// </summary>
        public static void UpdateParentSection(this I_BPG_Block self) {
            var parentSection = self.RectTransform.GetComponentInParent<I_BPG_BlockSection>();
            self.SetParentSection(parentSection);
        }


        #endregion


        /// ----------------------------------------------------------------------------
        #region Connection

        /// <summary>
        /// 指定された親ブロックに接続する．
        /// </summary>
        public static bool AppendTo(this I_BPG_Block self, I_BPG_Block parentBlock, int sectionIndex = 0, int siblingIndex = 0) {
            var sectionArray = parentBlock.Layout.Sections;
            if (sectionArray is null || sectionIndex.IsOutOfRange(sectionArray) || sectionArray[sectionIndex].Body is null) {
                return false;
            }

            // 接続
            var sectionBody = sectionArray[sectionIndex].Body;
            sectionBody.Append(self, siblingIndex);

            return true;
        }

        /// <summary>
        /// 指定されたブロックの親に接続する．配置場所は指定されたブロックの後ろ
        /// </summary>
        public static bool ConnectTo(this I_BPG_Block self, I_BPG_Block targetBlock) {
            if (!targetBlock.HasParentBlock()) return false;

            var index = targetBlock.GetIndexInSection();
            if (index < 0) return false;

            targetBlock.ParentSection.Body.Append(self, index + 1);
            return true;
        }


        #endregion
    }
}

