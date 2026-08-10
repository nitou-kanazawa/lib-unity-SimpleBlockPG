using System;
using System.Threading;
using System.Xml.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using nitou.BlockPG.Interface;

namespace nitou.BlockPG.Serialization {

    public static class BPG_BlockSerializer {

        /// ----------------------------------------------------------------------------
        #region Public Method (Convert : Blocks => XML)

        /// <summary>
        /// Convert <see cref="I_BPG_Block">block</see> to XML element.
        /// </summary>
        public static XElement BlockToXML(I_BPG_Block block) {
            if (block == null)
                throw new System.ArgumentNullException(nameof(block));

            var sBlock = BlockToSerializableBlock(block);
            return SerializableBlock.ToXElement(sBlock);
        }

        /// <summary>
        /// Convert <see cref="I_BE2_Block">block</see> to serializable block.
        /// </summary>
        public static SerializableBlock BlockToSerializableBlock(I_BPG_Block block) {

            // serializable block
            var sBlock = new SerializableBlock(
                id: block.GetGameObjectID(),
                name: block.RectTransform.name,
                localPosition: block.RectTransform.localPosition);

            // Custom Data
            //sBlock.blockIns = block.GetAdditonalData();

            // Section conversion
            foreach (var section in block.Layout.Sections) {

                var sSection = new SerializableBlockSection();

                // input
                //foreach (var input in section.Header.Inputs) {

                //    if (input.Transform.TryGetComponent<I_BPG_Block>(out _)) {
                //        Debug.LogWarning("ブロックの入力スポットに他ブロックが設定されています。");
                //    }

                //    sSection.inputs.Add(new SerializableInput(input.InputValues.stringValue));
                //}

                // blocks
                if (section.Body != null) {
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


        public static I_BPG_Block SerializableBlockToBlock(SerializableBlock sBlock, I_BPG_ProgrammingEnv programmingEnv) {
            if (sBlock == null)
                return null;
            if (programmingEnv == null)
                throw new ArgumentNullException(nameof(programmingEnv));

            I_BPG_Block block = null;

            var prefabName = sBlock.name;
            var prefab = BPG_BlockUtils.LoadBlockPrefab(prefabName);
            if (prefab != null) {

                block = BPG_BlockUtils.CreateBlock(prefab, programmingEnv);
                block.RectTransform.localPosition = sBlock.localPosition;

                //if (sBlock.blockIns != null) {
                //    block.SetAdditonalData(sBlock.blockIns); // ※カスタムブロックの固有データを設定する
                //}

                // 配下のセクションを生成する
                // [NOTE] 生成待ちの間にブロックが破棄されると復元処理が不正になるため、
                //        対象ブロックの破棄をキャンセル条件として渡す．
                var cancellationToken = block.RectTransform.gameObject.GetCancellationTokenOnDestroy();
                AddSerializableBlockSectionsAsync(block, sBlock, programmingEnv, cancellationToken).Forget();
            }

            return block;
        }

        /// <summary>
        /// セクションは以下のブロックを生成する．
        /// ※子ブロックは親セクションのインスタンス生成後に処理しないとNullエラーが発生する．
        /// </summary>
        private static async UniTaskVoid AddSerializableBlockSectionsAsync(I_BPG_Block block, SerializableBlock sBlock,
            I_BPG_ProgrammingEnv programmingEnv, CancellationToken cancellationToken) {

            // フレームの終了を待つ
            await UniTask.Yield(cancellationToken);

            if (block.Layout == null) {
                Debug.LogWarning($"Block layout is not found. (block: {sBlock.name})");
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

                var body = sections[s].Body;
                if (body != null) {
                    // Add children
                    foreach (var sChaildBlock in sBlock.sections[s].childBlocks) {
                        var childBlock = SerializableBlockToBlock(sChaildBlock, programmingEnv);

                        // ※プレハブが見つからない場合は null が返る
                        if (childBlock == null) {
                            Debug.LogWarning($"Failed to restore child block. (name: {sChaildBlock?.name})");
                            continue;
                        }

                        body.Append(childBlock);
                    }
                }

                var header = sections[s].Header;
                if (header != null) {
                    header.UpdateItems();
                    header.UpdateInputs();
                }
            }

        }

        #endregion
    }
}
