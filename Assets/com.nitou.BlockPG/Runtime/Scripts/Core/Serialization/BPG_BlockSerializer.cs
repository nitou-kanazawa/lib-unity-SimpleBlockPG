using System;
using System.Xml.Linq;
using UnityEngine;
using nitou.BlockPG.Interface;

namespace nitou.BlockPG.Serialization {

    /// <summary>
    /// ブロック階層と<see cref="XElement"/>の相互変換を行う．
    /// </summary>
    public static class BPG_BlockSerializer {

        // [NOTE] 復元処理はすべて同期的に完結する（1フレームで木構造全体が組み上がる）．
        //
        //  以前は階層ごとに UniTask.Yield() を挟む fire-and-forget だったため、
        //  深さNの木の復元にNフレームかかり、呼び出し側は完了を待てなかった．
        //  待機していた理由は「子ブロックは親セクションのインスタンス生成後でないとNullになる」ためだが、
        //  Object.Instantiate() はアクティブなオブジェクトの Awake()/OnEnable() を
        //  呼び出しの中で同期的に完了させる．本ライブラリで復元に必要な参照は
        //
        //    - BPG_BlockBase.Awake()          -> Layout
        //    - BPG_BlockVerticalLayout.Awake() -> Sections
        //    - BPG_BlockSection.Awake()        -> Header / Body の Initialize()
        //
        //  のいずれも Awake() で確定するため、Instantiate() の戻り時点で参照可能になっている．
        //  （Start() で行われるのは初期サイズの反映のみで、構造の組み立てには関与しない）

        /// ----------------------------------------------------------------------------
        #region Public Method (Convert : Blocks => XML)

        /// <summary>
        /// <see cref="I_BPG_Block">block</see> を XML要素へ変換する．
        /// </summary>
        public static XElement BlockToXML(I_BPG_Block block) {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            var sBlock = BlockToSerializableBlock(block);
            return SerializableBlock.ToXElement(sBlock);
        }

        /// <summary>
        /// <see cref="I_BPG_Block">block</see> をシリアライズ用オブジェクトへ変換する．
        /// </summary>
        public static SerializableBlock BlockToSerializableBlock(I_BPG_Block block) {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            // serializable block
            var sBlock = new SerializableBlock(
                id: block.Id,
                name: block.RectTransform.name,
                localPosition: block.RectTransform.localPosition);

            // Custom Data
            //sBlock.blockIns = block.GetAdditonalData();

            // [NOTE] Layout が無いブロックは子階層を持てないため、セクション変換をスキップする．
            if (block.Layout == null) {
                Debug.LogWarning($"Block layout is not found. Sections are not saved. (block: {sBlock.name})");
                return sBlock;
            }

            // Section conversion
            foreach (var section in block.Layout.Sections) {

                var sSection = new SerializableBlockSection();

                // input
                //foreach (var input in section.Header.Inputs) {
                //    sSection.inputs.Add(new SerializableInput(input.InputValues.stringValue));
                //}

                // blocks
                if (section?.Body != null) {
                    foreach (var childBlock in section.Body.ChildBlocks) {
                        sSection.childBlocks.Add(BlockToSerializableBlock(childBlock));
                    }
                }

                // Register to parent block
                sBlock.sections.Add(sSection);
            }
            return sBlock;
        }
        #endregion


        /// ----------------------------------------------------------------------------
        #region Public Method (Convert : XML => Blocks)

        /// <summary>
        /// XML要素からブロック階層を復元する．
        /// [NOTE] 生成は同期的に完了するため、戻り値の時点で子孫まで組み上がっている．
        /// </summary>
        public static I_BPG_Block XmlToBlock(XElement xBlock, I_BPG_ProgrammingEnv programmingEnv) {
            if (xBlock == null)
                throw new ArgumentNullException(nameof(xBlock));

            var sBlock = SerializableBlock.FromXElement(xBlock);
            return SerializableBlockToBlock(sBlock, programmingEnv);
        }

        /// <summary>
        /// シリアライズ用オブジェクトからブロック階層を復元する．
        /// [NOTE] 生成は同期的に完了するため、戻り値の時点で子孫まで組み上がっている．
        /// </summary>
        public static I_BPG_Block SerializableBlockToBlock(SerializableBlock sBlock, I_BPG_ProgrammingEnv programmingEnv) {
            if (sBlock == null)
                return null;
            if (programmingEnv == null)
                throw new ArgumentNullException(nameof(programmingEnv));

            var prefab = BPG_BlockUtils.LoadBlockPrefab(sBlock.name);
            if (prefab == null) {
                // ※LoadBlockPrefab側で警告を出力済み
                return null;
            }

            var block = BPG_BlockUtils.CreateBlock(prefab, programmingEnv);
            block.RectTransform.localPosition = sBlock.localPosition;

            // 保存時の識別IDを復元する
            if (!string.IsNullOrEmpty(sBlock.id)) {
                block.SetId(sBlock.id);
            }

            //if (sBlock.blockIns != null) {
            //    block.SetAdditonalData(sBlock.blockIns); // ※カスタムブロックの固有データを設定する
            //}

            // 配下のセクションを復元する
            RestoreSections(block, sBlock, programmingEnv);

            return block;
        }
        #endregion


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// セクション以下のブロックを復元する．
        /// </summary>
        private static void RestoreSections(I_BPG_Block block, SerializableBlock sBlock,
            I_BPG_ProgrammingEnv programmingEnv) {

            // [NOTE] プレハブのルートが非アクティブな場合、Instantiate() は Awake() を呼ばないため
            //        参照が未解決のままになる．その場合は子階層を復元できない．
            if (block.Layout == null) {
                Debug.LogWarning($"Block layout is not found. Child blocks are not restored. " +
                    $"(block: {sBlock.name}) Check that the prefab root is active.");
                return;
            }

            var sections = block.Layout.Sections;

            // [NOTE] プレハブ側のセクション構成が保存時から変わっていると個数が食い違う．
            //        処理できる範囲だけ復元し、欠落は警告として通知する．
            if (sections.Count != sBlock.sections.Count) {
                Debug.LogWarning($"Section count does not match the saved data. " +
                    $"(block: {sBlock.name}, prefab: {sections.Count}, saved: {sBlock.sections.Count})");
            }

            int sectionCount = Mathf.Min(sections.Count, sBlock.sections.Count);
            for (int s = 0; s < sectionCount; s++) {

                var section = sections[s];
                if (section == null)
                    continue;

                var body = section.Body;
                if (body != null) {
                    // Add children
                    foreach (var sChildBlock in sBlock.sections[s].childBlocks) {
                        var childBlock = SerializableBlockToBlock(sChildBlock, programmingEnv);

                        // ※プレハブが見つからない場合は null が返る
                        if (childBlock == null) {
                            Debug.LogWarning($"Failed to restore child block. (name: {sChildBlock?.name})");
                            continue;
                        }

                        body.AppendLast(childBlock);
                    }
                }

                var header = section.Header;
                if (header != null) {
                    header.UpdateItems();
                    header.UpdateInputs();
                }
            }
        }
    }
}
