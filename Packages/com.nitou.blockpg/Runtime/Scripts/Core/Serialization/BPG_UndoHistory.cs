using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using nitou.BlockPG.Interface;

namespace nitou.BlockPG.Serialization {

    /// <summary>
    /// ワークスペースの操作履歴．
    /// </summary>
    /// <remarks>
    /// [NOTE] 操作ごとの逆操作を記録するのではなく、状態のスナップショットを積む方式を取る．
    ///
    ///        現状の接続APIは Transform の再ペアレントを直接行う形で、逆操作を記録する
    ///        仕組みが無い．細粒度の記録に変えるには接続まわりのAPIを全面的に見直す必要があり、
    ///        まずは単純で壊れにくいこの方式から始める．
    ///
    ///        復元はフレームをまたがずに完了するため、Undo のたびに操作を
    ///        受け付けられない時間が生まれることはない．
    ///
    ///        復元するとブロックのインスタンスは作り直される．
    ///        選択状態などを保持する場合は、参照ではなく識別IDで持つこと．
    ///        識別IDは復元をまたいで維持される．
    /// </remarks>
    public sealed class BPG_UndoHistory {

        private readonly I_BPG_ProgrammingEnv _programmingEnv;
        private readonly int _capacity;

        // ※末尾が最新
        private readonly List<Snapshot> _undo = new();
        private readonly List<Snapshot> _redo = new();

        /// <summary>
        /// 復元を行った直後に通知する．（※引数は操作の名前）
        /// </summary>
        public event Action<string> OnRestored;


        private readonly struct Snapshot {
            public readonly string Label;
            public readonly string Xml;

            public Snapshot(string label, string xml) {
                Label = label;
                Xml = xml;
            }
        }


        /// ----------------------------------------------------------------------------
        // Property

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public int UndoCount => _undo.Count;
        public int RedoCount => _redo.Count;

        /// <summary>
        /// 次に取り消される操作の名前．（※履歴が無い場合はnull）
        /// </summary>
        public string NextUndoLabel => CanUndo ? _undo[^1].Label : null;

        /// <summary>
        /// 次にやり直される操作の名前．（※履歴が無い場合はnull）
        /// </summary>
        public string NextRedoLabel => CanRedo ? _redo[^1].Label : null;


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <param name="capacity">保持する履歴の数．超えた古い履歴から捨てる．</param>
        public BPG_UndoHistory(I_BPG_ProgrammingEnv programmingEnv, int capacity = 50) {
            if (programmingEnv == null)
                throw new ArgumentNullException(nameof(programmingEnv));
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _programmingEnv = programmingEnv;
            _capacity = capacity;
        }

        /// <summary>
        /// 現在の状態を履歴へ積む．
        /// </summary>
        /// <remarks>
        /// **操作を行う前に**呼ぶこと．Undo はここで記録した状態へ戻す．
        /// やり直しの履歴は破棄される．
        ///
        /// [NOTE] 直前の記録から状態が変わっていなければ積まない．
        ///        「掴んだが動かさなかった」ような操作で履歴が埋まるのを防ぐ．
        /// </remarks>
        public void Record(string label) {
            var xml = Capture();
            if (_undo.Count > 0 && _undo[^1].Xml == xml)
                return;

            _undo.Add(new Snapshot(label ?? string.Empty, xml));
            _redo.Clear();

            // ※古い履歴から捨てる
            while (_undo.Count > _capacity) {
                _undo.RemoveAt(0);
            }
        }

        /// <summary>
        /// 直前の操作を取り消す．
        /// </summary>
        /// <returns>取り消した場合はtrue．履歴が無い場合はfalse．</returns>
        public bool Undo() {
            return Step(_undo, _redo);
        }

        /// <summary>
        /// 取り消した操作をやり直す．
        /// </summary>
        /// <returns>やり直した場合はtrue．履歴が無い場合はfalse．</returns>
        public bool Redo() {
            return Step(_redo, _undo);
        }

        /// <summary>
        /// 履歴を破棄する．
        /// </summary>
        public void Clear() {
            _undo.Clear();
            _redo.Clear();
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        private bool Step(List<Snapshot> from, List<Snapshot> to) {
            if (from.Count == 0)
                return false;

            var target = from[^1];
            from.RemoveAt(from.Count - 1);

            // ※戻る前に現在の状態を反対側へ積む
            to.Add(new Snapshot(target.Label, Capture()));

            Restore(target.Xml);
            OnRestored?.Invoke(target.Label);
            return true;
        }

        private string Capture() {
            var document = BPG_BlockStorage.BlocksToXDocument(_programmingEnv.GetRootBlocks());
            return document.ToString(SaveOptions.DisableFormatting);
        }

        private void Restore(string xml) {
            IReadOnlyList<SerializableBlock> sBlocks;
            try {
                sBlocks = BPG_BlockStorage.FromXDocument(XDocument.Parse(xml));
            }
            // [NOTE] 自前で書き出した文字列のため通常は失敗しないが、
            //        失敗した場合に現在の状態まで失わないよう、復元を中止する．
            catch (System.Xml.XmlException e) {
                Debug.LogError($"Failed to restore the snapshot. {e.Message}");
                return;
            }

            _programmingEnv.RemoveAllBlocks();

            foreach (var sBlock in sBlocks) {
                BPG_BlockSerializer.SerializableBlockToBlock(sBlock, _programmingEnv);
            }
        }
    }
}
