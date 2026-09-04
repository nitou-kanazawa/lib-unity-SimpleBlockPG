using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Collections.Generic;
using UnityEngine;
using nitou.BlockPG.Interface;

namespace nitou.BlockPG.Serialization {

    /// <summary>
    /// ブロック階層の保存・読み込みを行う．
    /// </summary>
    public static class BPG_BlockStorage {

        // [NOTE] 非同期APIの責務分担
        //
        //  ファイルI/OとXMLの解析はUnityのAPIに触れないため、スレッドプールで実行する．
        //  一方、ブロックの生成はメインスレッドでしか行えないが同期的に完結するため、
        //  メインスレッドへ戻ってから1フレーム内でまとめて組み上げる．
        //
        //  → 呼び出し側は await で完了を待てて、かつ生成はフレームをまたがない．
        //
        // [NOTE] WebGL はスレッドを持たないため Task.Run が使えない．
        //        当該プラットフォームでは同期実行へフォールバックする．
        //        （ファイルI/O自体は persistentDataPath 経由で動作する）
        //
        // [NOTE] 非同期APIは UniTask ではなく標準の Task を使う．ライブラリの依存を減らすため．
        //        Unity はメインスレッドに SynchronizationContext を敷いているため、
        //        メインスレッドから await すれば継続もメインスレッドへ戻る．
        //        （UniTask.SwitchToMainThread に相当する明示的な切り替えは要らない）

        /// <summary>
        /// ルート要素の識別名．
        /// </summary>
        public static readonly string ROOT_KEY = "BlockPG";

        /// <summary>
        /// 保存形式のバージョン．（※将来のマイグレーション用）
        /// </summary>
        public static readonly string FORMAT_VERSION = "1";

        private static readonly string VERSION_ATTRIBUTE = "version";


        /// ----------------------------------------------------------------------------
        #region Public Method (Path)

        /// <summary>
        /// 既定の保存先パスを取得する．
        /// </summary>
        public static string GetDefaultPath(string fileName) {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name must not be null or empty.", nameof(fileName));

            if (!Path.HasExtension(fileName)) {
                fileName += ".xml";
            }
            return Path.Combine(Application.persistentDataPath, "BlockPG", fileName);
        }

        /// <summary>
        /// 保存データが存在するか判定する．
        /// </summary>
        public static bool Exists(string path) {
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        /// <summary>
        /// 保存データを削除する．（※存在しない場合は false を返す）
        /// </summary>
        public static bool Delete(string path) {
            if (!Exists(path))
                return false;

            File.Delete(path);
            return true;
        }
        #endregion


        /// ----------------------------------------------------------------------------
        #region Public Method (Convert : XDocument)

        /// <summary>
        /// ブロック階層を<see cref="XDocument"/>へ変換する．
        /// </summary>
        public static XDocument BlocksToXDocument(IEnumerable<I_BPG_Block> blocks) {
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));

            var sBlocks = blocks
                .Where(block => block != null)
                .Select(BPG_BlockSerializer.BlockToSerializableBlock);

            return ToXDocument(sBlocks);
        }

        /// <summary>
        /// シリアライズ用オブジェクトを<see cref="XDocument"/>へ変換する．
        /// </summary>
        public static XDocument ToXDocument(IEnumerable<SerializableBlock> sBlocks) {
            if (sBlocks == null)
                throw new ArgumentNullException(nameof(sBlocks));

            var xRoot = new XElement(ROOT_KEY,
                new XAttribute(VERSION_ATTRIBUTE, FORMAT_VERSION));

            foreach (var sBlock in sBlocks.Where(b => b != null)) {
                xRoot.Add(SerializableBlock.ToXElement(sBlock));
            }

            return new XDocument(new XDeclaration("1.0", "utf-8", null), xRoot);
        }

        /// <summary>
        /// <see cref="XDocument"/>をシリアライズ用オブジェクトへ変換する．
        /// </summary>
        public static IReadOnlyList<SerializableBlock> FromXDocument(XDocument document) {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var xRoot = document.Root;
            if (xRoot == null) {
                Debug.LogWarning("Save data has no root element. Returning empty list.");
                return Array.Empty<SerializableBlock>();
            }

            // [NOTE] ルート名が想定と違ってもブロック要素があれば読み進める．
            if (xRoot.Name != ROOT_KEY) {
                Debug.LogWarning($"Unexpected root element. (expected: {ROOT_KEY}, actual: {xRoot.Name})");
            }

            return xRoot.Elements(SerializableBlock.NAME_KEY)
                .Select(SerializableBlock.FromXElement)
                .ToArray();
        }
        #endregion


        /// ----------------------------------------------------------------------------
        #region Public Method (Save)

        /// <summary>
        /// ブロック階層をファイルへ保存する．
        /// </summary>
        public static void Save(string path, IEnumerable<I_BPG_Block> blocks) {
            WriteToFile(path, BlocksToXDocument(blocks));
        }

        /// <summary>
        /// ブロック階層をファイルへ非同期に保存する．
        /// [NOTE] XMLの構築はメインスレッド、ファイル書き込みはスレッドプールで行う．
        /// </summary>
        public static async Task SaveAsync(string path, IEnumerable<I_BPG_Block> blocks,
            CancellationToken cancellationToken = default) {

            // ※Unityオブジェクトを読むため、変換はメインスレッドで行う
            var document = BlocksToXDocument(blocks);

            cancellationToken.ThrowIfCancellationRequested();

#if UNITY_WEBGL && !UNITY_EDITOR
            WriteToFile(path, document);
            await Task.CompletedTask;
#else
            await Task.Run(() => WriteToFile(path, document), cancellationToken);
#endif
        }

        /// <summary>
        /// シリアライズ用オブジェクトをファイルへ保存する．
        /// </summary>
        public static void SaveSerializableBlocks(string path, IEnumerable<SerializableBlock> sBlocks) {
            WriteToFile(path, ToXDocument(sBlocks));
        }
        #endregion


        /// ----------------------------------------------------------------------------
        #region Public Method (Load)

        /// <summary>
        /// ファイルからブロック階層を復元する．
        /// [NOTE] 生成は同期的に完了するため、戻り値の時点で子孫まで組み上がっている．
        /// </summary>
        public static IReadOnlyList<I_BPG_Block> Load(string path, I_BPG_ProgrammingEnv programmingEnv) {
            if (programmingEnv == null)
                throw new ArgumentNullException(nameof(programmingEnv));

            var sBlocks = LoadSerializableBlocks(path);
            return InstantiateBlocks(sBlocks, programmingEnv);
        }

        /// <summary>
        /// ファイルからブロック階層を非同期に復元する．
        /// [NOTE] ファイル読み込みとXML解析はスレッドプールで行い、
        ///        ブロックの生成はメインスレッドで1フレーム内に完了する．
        /// </summary>
        public static async Task<IReadOnlyList<I_BPG_Block>> LoadAsync(string path,
            I_BPG_ProgrammingEnv programmingEnv, CancellationToken cancellationToken = default) {

            if (programmingEnv == null)
                throw new ArgumentNullException(nameof(programmingEnv));

#if UNITY_WEBGL && !UNITY_EDITOR
            var sBlocks = LoadSerializableBlocks(path);
            await Task.CompletedTask;
#else
            var sBlocks = await Task.Run(() => LoadSerializableBlocks(path), cancellationToken);
#endif

            // ※ブロックの生成はメインスレッドでしか行えない．
            //   await が Unity の SynchronizationContext を捕捉するため、ここは既に戻っている．
            cancellationToken.ThrowIfCancellationRequested();

            return InstantiateBlocks(sBlocks, programmingEnv);
        }

        /// <summary>
        /// ファイルからシリアライズ用オブジェクトを読み込む．
        /// ファイルが存在しない、または解析できない場合は空のリストを返す．
        /// </summary>
        public static IReadOnlyList<SerializableBlock> LoadSerializableBlocks(string path) {
            if (!Exists(path)) {
                Debug.LogWarning($"Save data is not found. (path: {path})");
                return Array.Empty<SerializableBlock>();
            }

            XDocument document;
            try {
                document = XDocument.Parse(File.ReadAllText(path));
            }
            // [NOTE] 保存データはユーザー環境で破損しうるため、例外で停止させず空データとして扱う．
            catch (Exception e) when (e is System.Xml.XmlException || e is IOException) {
                Debug.LogWarning($"Failed to read save data. (path: {path}) {e.Message}");
                return Array.Empty<SerializableBlock>();
            }

            return FromXDocument(document);
        }
        #endregion


        /// ----------------------------------------------------------------------------
        // Private Method

        private static void WriteToFile(string path, XDocument document) {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null or empty.", nameof(path));

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) {
                Directory.CreateDirectory(directory);
            }

            document.Save(path);
        }

        private static IReadOnlyList<I_BPG_Block> InstantiateBlocks(
            IReadOnlyList<SerializableBlock> sBlocks, I_BPG_ProgrammingEnv programmingEnv) {

            var blocks = new List<I_BPG_Block>(sBlocks.Count);
            foreach (var sBlock in sBlocks) {
                var block = BPG_BlockSerializer.SerializableBlockToBlock(sBlock, programmingEnv);

                // ※プレハブが見つからない場合は null が返る
                if (block != null) {
                    blocks.Add(block);
                }
            }
            return blocks;
        }
    }
}
