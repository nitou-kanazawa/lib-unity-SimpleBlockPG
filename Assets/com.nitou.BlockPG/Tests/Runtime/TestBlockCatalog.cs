using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.Blocks.Section;
using nitou.BlockPG.Interface;

namespace RuntimeTests {

    /// <summary>
    /// 検証用のカタログ．コード上で組み立てたブロックを返す．
    /// </summary>
    /// <remarks>
    /// [NOTE] これが成立することが #33 の狙い．実資産（Resources のプレハブ）に
    ///        依存せずに、生成と復元の経路を検証できる．
    /// </remarks>
    public sealed class TestBlockCatalog : I_BPG_BlockCatalog, System.IDisposable {

        private readonly Dictionary<string, I_BPG_Block> _prefabs = new();
        private readonly GameObject _holder;

        /// <summary>
        /// 問い合わせられたブロック名．
        /// </summary>
        public List<string> RequestedNames { get; } = new();


        /// ----------------------------------------------------------------------------
        // Public Method

        public TestBlockCatalog() {
            // [NOTE] 非アクティブな親の下に置く．プレハブと同じく、生成されるまで
            //        Awake を走らせないため．（自身を非アクティブにすると、
            //        生成された実体まで非アクティブになってしまう）
            _holder = new GameObject("[Test] PrefabHolder");
            _holder.SetActive(false);
        }

        public void Dispose() {
            if (_holder != null) {
                Object.DestroyImmediate(_holder);
            }
        }

        /// <summary>
        /// ブロック名に対応するプレハブを取得する．
        /// </summary>
        public I_BPG_Block GetPrefab(string blockName) {
            RequestedNames.Add(blockName);
            return _prefabs.TryGetValue(blockName, out var prefab) ? prefab : null;
        }

        /// <summary>
        /// コード上で組み立てたブロックを登録する．
        /// </summary>
        public I_BPG_Block Register(string blockName) {
            var prefab = Build(blockName);
            _prefabs[blockName] = prefab;
            return prefab;
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// 最小構成のブロックを組み立てる．（Block > Section > Header）
        /// </summary>
        private I_BPG_Block Build(string blockName) {
            var root = new GameObject(blockName, typeof(RectTransform));
            root.transform.SetParent(_holder.transform, false);
            root.AddComponent<BPG_Block>();
            root.AddComponent<BPG_BlockVerticalLayout>();

            var section = new GameObject("Section", typeof(RectTransform));
            section.transform.SetParent(root.transform, false);
            var sectionComponent = section.AddComponent<BPG_BlockSection>();

            var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(section.transform, false);
            var headerComponent = header.AddComponent<BPG_BlockSectionHeader>();

            // ※セクションはヘッダーとボディをシリアライズ済みの参照で持つ
            SetPrivateField(sectionComponent, "_header", headerComponent);

            return root.GetComponent<I_BPG_Block>();
        }

        private static void SetPrivateField(object target, string fieldName, object value) {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
                throw new System.InvalidOperationException($"Field not found. ({fieldName})");

            field.SetValue(target, value);
        }
    }


    /// <summary>
    /// 検証用のファクトリ．生成の回数を数えるだけで、実体の作り方は既定と同じ．
    /// </summary>
    public sealed class TestBlockFactory : I_BPG_BlockFactory {

        private readonly I_BPG_BlockFactory _inner = new BPG_DefaultBlockFactory();

        /// <summary>生成が要求された回数．</summary>
        public int CreatedCount { get; private set; }

        /// <summary>生成を失敗させるかどうか．</summary>
        public bool ShouldFail { get; set; }

        public I_BPG_Block Create(I_BPG_Block prefab, RectTransform parent) {
            CreatedCount++;
            return ShouldFail ? null : _inner.Create(prefab, parent);
        }
    }
}
