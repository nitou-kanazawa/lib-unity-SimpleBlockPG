using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace nitou.BlockPG.BuildTools {

    /// <summary>
    /// WebGLデモのビルドを実行する．
    /// CIからは -executeMethod nitou.BlockPG.BuildTools.WebGLBuilder.Build で呼び出す．
    /// </summary>
    public static class WebGLBuilder {

        /// <summary>
        /// ビルド対象シーン．
        /// </summary>
        // [NOTE] EditorBuildSettings は開発中に頻繁に書き換わるため参照しない．
        //        公開されるデモの内容はここで固定する．
        // ※先頭が起動シーンになる
        private static readonly string[] SCENES = {
            "Packages/com.nitou.blockpg/Samples/Demo/00-Hub/00-Hub.unity",
            "Packages/com.nitou.blockpg/Samples/Demo/06-Playground/06-Playground.unity",
            "Packages/com.nitou.blockpg/Samples/Demo/07-InputBlocks/07-InputBlocks.unity",
        };

        /// <summary>
        /// 出力先の既定値．（※リポジトリルートからの相対パス）
        /// </summary>
        private const string DEFAULT_OUTPUT_DIRECTORY = "build/WebGL";

        /// <summary>
        /// GameCIが出力先を渡してくる引数名．
        /// </summary>
        private const string CUSTOM_BUILD_PATH_ARG = "-customBuildPath";


        /// ----------------------------------------------------------------------------
        #region Public Method

        /// <summary>
        /// バッチモードからの実行エントリ．
        /// </summary>
        public static void Build() {
            var outputDirectory = ResolveOutputDirectory();
            var summary = BuildInternal(outputDirectory).summary;

            Debug.Log($"WebGL build {summary.result}. " +
                $"(output: {outputDirectory}, size: {summary.totalSize / (1024 * 1024)}MB, time: {summary.totalTime})");

            if (summary.result == BuildResult.Succeeded)
                return;

            // [NOTE] バッチモードでは例外を投げても終了コードが0になる場合があるため、
            //        CIが失敗を検知できるよう明示的に終了させる．
            if (Application.isBatchMode) {
                EditorApplication.Exit(1);
                return;
            }
            throw new Exception($"WebGL build failed. (result: {summary.result})");
        }

        /// <summary>
        /// エディタからの手動実行エントリ．
        /// </summary>
        [MenuItem("Tools/BlockPG/Build WebGL Demo")]
        public static void BuildFromMenu() {
            var outputDirectory = Path.GetFullPath(DEFAULT_OUTPUT_DIRECTORY);
            if (BuildInternal(outputDirectory).summary.result == BuildResult.Succeeded) {
                EditorUtility.RevealInFinder(outputDirectory);
            }
        }
        #endregion


        /// ----------------------------------------------------------------------------
        // Private Method

        private static BuildReport BuildInternal(string outputDirectory) {
            ApplyStaticHostingSettings();
            AssertScenesExist();

            var options = new BuildPlayerOptions {
                scenes = SCENES,
                locationPathName = outputDirectory,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };
            return BuildPipeline.BuildPlayer(options);
        }

        /// <summary>
        /// 静的ホスティングで動作する構成を強制する．
        /// </summary>
        // [NOTE] Brotli圧縮のままだと、サーバが Content-Encoding を返さない限り
        //        ブラウザがロードに失敗する．GitHub Pages はヘッダを設定できないため、
        //        Unity同梱のデコンプレッサで解けるGzip + フォールバック構成にする．
        //        ProjectSettings にも同じ値を保存してあるが、エディタ上で戻されても
        //        成果物が壊れないようビルド時にも設定する．
        private static void ApplyStaticHostingSettings() {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
        }

        /// <summary>
        /// ビルド対象シーンの存在を確認する．
        /// </summary>
        // [NOTE] 存在しないパスを渡すとUnityは警告を出したうえで空のシーンをビルドしてしまう．
        //        中身の無いデモが公開されるのを防ぐため、事前に落とす．
        private static void AssertScenesExist() {
            var missing = SCENES
                .Where(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                .ToArray();

            if (missing.Length == 0)
                return;

            throw new FileNotFoundException($"Build scenes are not found. ({string.Join(", ", missing)})");
        }

        /// <summary>
        /// 出力先ディレクトリを決定する．
        /// </summary>
        private static string ResolveOutputDirectory() {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) {
                if (args[i] == CUSTOM_BUILD_PATH_ARG) {
                    return args[i + 1];
                }
            }
            return Path.GetFullPath(DEFAULT_OUTPUT_DIRECTORY);
        }
    }
}
