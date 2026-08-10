using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using nitou.BlockPG.Enviorment;
using nitou.BlockPG.Interface;

// [NOTE] BPG_BlockUtils は nitou.BlockPG（生成）と nitou.BlockPG.Blocks（破棄）の
//        2つの名前空間に同名で存在するため、明示的に解決する．
using BlockUtils = nitou.BlockPG.BPG_BlockUtils;

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
        }


        private readonly List<GameObject> _roots = new();

        /// <summary>
        /// ブロックの配置先．
        /// </summary>
        public I_BPG_ProgrammingEnv ProgrammingEnv { get; }


        /// ----------------------------------------------------------------------------
        // Public Method

        public BlockPGTestEnv() {
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
            var prefab = BlockUtils.LoadBlockPrefab(prefabName);
            if (prefab == null)
                throw new InvalidOperationException($"Block prefab is not found. (name: {prefabName})");

            return BlockUtils.CreateBlock(prefab, ProgrammingEnv);
        }

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
