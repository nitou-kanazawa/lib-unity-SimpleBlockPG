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
        /// ブロックの機能実装．
        /// </summary>
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
        /// ルートブロックかどうか判定する．
        /// </summary>
        public static bool IsRootBlock(this I_BPG_Block self) {
            return self.ParentSection == null;
        }

        /// <summary>
        /// 親セクション内の最初のブロックかどうか判定する．
        /// ParentSectionがnullの場合はfalseを返す．
        /// </summary>
        public static bool IsFirstBlockInSection(this I_BPG_Block self) {
            // ParentSectionがnullの場合
            if (!self.HasParentBlock()) return false;

            var sectionBody = self.ParentSection.Body;
            if (sectionBody.ChildBlocks.Count != sectionBody.RectTransform.childCount) {
                Debug.LogWarning("");
            }

            return sectionBody.ChildBlocks.First() == self;
        }

        /// <summary>
        /// 親セクション内の最初のブロックかどうか判定する．
        /// ParentSectionがnullの場合はfalseを返す．
        /// </summary>
        public static bool IsLastBlockInSection(this I_BPG_Block self) {
            // ParentSectionがnullの場合
            if (!self.HasParentBlock()) return false;

            // 
            var sectionBody = self.ParentSection.Body;
            if (sectionBody.ChildBlocks.Count != sectionBody.RectTransform.childCount) {
                Debug.LogWarning("");
            }

            return sectionBody.ChildBlocks.Last() == self;
        }

        /// <summary>
        /// 親セクション内でのインデックスを取得する．
        /// ParentSectionがnullの場合は-1を返す．
        /// </summary>
        public static int GetIndexInSection(this I_BPG_Block self) {
            // ParentSectionがnullの場合
            if (!self.HasParentBlock()) return -1;

            var sectionBody = self.ParentSection.Body;
            if (sectionBody.ChildBlocks.Count != sectionBody.RectTransform.childCount) {
                Debug.LogWarning("");
            }

            return sectionBody.ChildBlocks.IndexOf(self);
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

        // [TODO] 実装 (※同じ親を持つ1つ前のブロック)
        public static I_BPG_Block GetPreviousBlock(this I_BPG_Block self) {
            if (!self.HasParentBlock()) return null;

            var sectionBody = self.ParentSection.Body;
            var index = sectionBody.ChildBlocks.IndexOf(self);

            // インデックスが有効範囲内であれば前の要素を返す
            return index > 0 ? sectionBody.ChildBlocks[index - 1] : null;
        }

        // [TODO] 実装 (※同じ親を持つ1つ後ろのブロック)
        public static I_BPG_Block GetNextBlock(this I_BPG_Block self) {
            if (!self.HasParentBlock()) return null;

            var sectionBody = self.ParentSection.Body;
            var index = sectionBody.ChildBlocks.IndexOf(self);

            // インデックスが有効範囲内であれば次の要素を返す
            return (0 <= index && index < sectionBody.ChildBlocks.Count -1) 
                ? sectionBody.ChildBlocks[index + 1] : null;
        }



        /// <summary>
        /// 子階層以下の<see cref="I_BE2_Block"/>を再帰的に取得する．
        /// </summary>
        public static List<I_BPG_Block> GetAllChaildBlocks(this I_BPG_Block self, bool containSelf = true) {
            if (self == null) return null;

            var blockList = new List<I_BPG_Block>();
            if (containSelf) {
                blockList.Add(self);
            }

            // 子階層以下
            var childBlocks = self.Layout.Sections
                .Where(section => section != null)
                .SelectMany(section => section.GetAllChaildBlocks());
            blockList.AddRange(childBlocks);
            return blockList;
        }

        /// <summary>
        /// 子階層以下の<see cref="I_BPG_Block"/>の数を取得する．
        /// </summary>
        public static int GetAllChaildBlocksCount(this I_BPG_Block self, bool containSelf = true) {
            if (self == null)
                throw new System.ArgumentNullException(nameof(self));

            // ※自分自信を含めるかどうか
            int additional = (containSelf ? 1 : 0);

            // 
            return additional +
                self.Layout.Sections
                .Where(section => section != null)
                .Select(section => section.GetAllChaildBlocksCount())
                .Sum();
        }

        /// <summary>
        /// Obtains a reference to the parent section to which it belongs.
        /// </summary>
        public static I_BPG_BlockSection GetFirstSection(this I_BPG_Block self) {
            return self.Layout.Sections.FirstOrDefault();
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
        public static bool AppendTo(this I_BPG_Block self, I_BPG_Block parentBlock, int sectionIndex = 0, int siblinIndex = 0) {
            var sectionArray = parentBlock.Layout.Sections;
            if (sectionArray is null || sectionIndex.IsOutOfRange(sectionArray) || sectionArray[sectionIndex].Body is null) {
                return false;
            }

            // 接続
            var sectionBody = sectionArray[sectionIndex].Body;
            sectionBody.Append(self, siblinIndex);

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

public static class IReadOnlyListExtensions {
    /// <summary>
    /// 指定された要素のインデックスを取得します。
    /// 見つからない場合は -1 を返します。
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
