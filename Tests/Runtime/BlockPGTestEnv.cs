using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using nitou.BlockPG.DragDrop;
using nitou.BlockPG.Environments;
using nitou.BlockPG.Interface;

using nitou.BlockPG.Blocks;

namespace RuntimeTests {

    /// <summary>
    /// ブロック操作のテストに必要な最小構成を組み立てる．
    /// [NOTE] ブロックの生成・接続は Canvas 配下の <see cref="I_BPG_ProgrammingEnv"/> を前提とするため、
    ///        テストごとに使い捨ての環境を用意する．using で破棄すること．
    /// </summary>
    public sealed class BlockPGTestEnv : IDisposable {

        /// <summary>
        /// テストで使用できるブロックプレハブ名．
        /// </summary>
        public static class PrefabName {
            /// <summary>トリガーブロック．子ブロックを持てる．</summary>
            public const string Entry = "Block [Entry]";
            /// <summary>単一セクションのブロック．Bodyを持たないため子ブロックは置けない．</summary>
            public const string Normal = "Block [Normal]";
            /// <summary>単一セクションのブロック．子ブロックを持てる．</summary>
            public const string Scope = "Block [Scope]";
            /// <summary>2セクションのブロック．各セクションに子ブロックを持てる．</summary>
            public const string MultiScope = "Block [MultiScope]";

            /// <summary>
            /// 検証用のブロック．ヘッダーに入力要素、ルートにブロック固有データの受け手を持つ．
            /// </summary>
            /// <remarks>
            /// [NOTE] 復元はプレハブからの生成で行うため、入力や固有データの受け手が
            ///        プレハブ側に無いと保存と復元を通しで検証できない．
            ///        同梱プレハブに入力を足すと見た目が変わるため、テスト専用に用意している．
            /// </remarks>
            public const string TestInput = "Block [TestInput]";
        }


        private readonly List<GameObject> _roots = new();

        /// <summary>
        /// ブロックの配置先．
        /// </summary>
        public I_BPG_ProgrammingEnv ProgrammingEnv { get; }


        /// <summary>
        /// ブロックが載る Canvas．
        /// </summary>
        public Canvas Canvas { get; }

        /// <summary>
        /// ドラッグ中のブロックの一時的な配置先．（※withDraggingSystem 指定時のみ）
        /// </summary>
        public RectTransform DraggingLayer { get; }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <param name="canvasScaleFactor">
        /// Canvas の表示倍率．CanvasScaler を使う実環境では 1 以外になるため、
        /// 生成物のスケールが倍率に影響されないことを検証する用途で指定する．
        /// </param>
        /// <param name="withDraggingSystem">
        /// ドラッグ操作に必要な <see cref="DraggingSystem"/> を用意するかどうか．
        /// </param>
        public BlockPGTestEnv(float canvasScaleFactor = 1f, bool withDraggingSystem = false) {
            // EventSystem
            // ※UI操作を伴わないテストでも、EventSystemが無いと一部のUGUI処理が警告を出す
            if (EventSystem.current == null) {
                var eventSystemObj = new GameObject("[Test] EventSystem",
                    typeof(EventSystem), typeof(StandaloneInputModule));
                _roots.Add(eventSystemObj);
            }

            // Canvas
            var canvasObj = new GameObject("[Test] Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = canvasScaleFactor;
            Canvas = canvas;
            _roots.Add(canvasObj);

            // ProgrammingEnv
            // [NOTE] 非アクティブな状態でコンポーネントを揃えてから有効化する．
            //        BPG_SpotProgrammingEnv は OnEnable() で I_BPG_ProgrammingEnv を取得するため、
            //        アクティブなまま AddComponent すると取得順が前後する．
            var envObj = new GameObject("[Test] ProgrammingEnv", typeof(RectTransform));
            envObj.SetActive(false);
            envObj.transform.SetParent(canvasObj.transform, worldPositionStays: false);

            var env = envObj.AddComponent<BPG_ProgrammingEnv>();  // ※CanvasGroup / Spot は RequireComponent で付く
            var envRect = envObj.GetComponent<RectTransform>();
            envRect.anchorMin = Vector2.zero;
            envRect.anchorMax = Vector2.one;
            envRect.offsetMin = Vector2.zero;
            envRect.offsetMax = Vector2.zero;

            envObj.SetActive(true);
            ProgrammingEnv = env;
            Env = env;

            if (!withDraggingSystem)
                return;

            // ドラッグ中の一時的な配置先
            var dragLayerObj = new GameObject("[Test] DraggingLayer", typeof(RectTransform));
            var dragRect = dragLayerObj.GetComponent<RectTransform>();
            dragRect.SetParent(canvasObj.transform, worldPositionStays: false);
            dragRect.anchorMin = Vector2.zero;
            dragRect.anchorMax = Vector2.one;
            dragRect.offsetMin = Vector2.zero;
            dragRect.offsetMax = Vector2.zero;
            DraggingLayer = dragRect;

            // DraggingSystem
            // ※ドラッグ処理は配置先が未設定だと親なしへ飛ばされて成立しない
            var systemObj = new GameObject("[Test] DraggingSystem");
            var system = systemObj.AddComponent<DraggingSystem>();
            system.Setup(dragRect);
            _roots.Add(systemObj);
        }

        public void Dispose() {
            // 環境より先にブロックを破棄する
            foreach (Transform child in ProgrammingEnv.RectTransform) {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            foreach (var root in _roots) {
                if (root != null) {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
            _roots.Clear();
        }


        /// ----------------------------------------------------------------------------
        // Helper

        /// <summary>
        /// 指定名のブロックを生成して環境へ配置する．
        /// </summary>
        public I_BPG_Block CreateBlock(string prefabName) {
            // ※環境のカタログを通す（差し替えたカタログもそのまま効く）
            var prefab = BPG_BlockUtils.LoadBlockPrefab(prefabName, ProgrammingEnv);
            if (prefab == null)
                throw new InvalidOperationException($"Block prefab is not found. (name: {prefabName})");

            return BPG_BlockUtils.CreateBlock(prefab, ProgrammingEnv);
        }

        /// <summary>
        /// ブロックの配置先．（※カタログやファクトリの差し替えに使う）
        /// </summary>
        public BPG_ProgrammingEnv Env { get; }

        /// <summary>
        /// 環境直下のルートブロックを取得する．
        /// </summary>
        public IReadOnlyList<I_BPG_Block> GetRootBlocks() {
            var blocks = new List<I_BPG_Block>();
            foreach (Transform child in ProgrammingEnv.RectTransform) {
                if (child.TryGetComponent<I_BPG_Block>(out var block)) {
                    blocks.Add(block);
                }
            }
            return blocks;
        }
    }
}
