using System;
using UnityEngine;

namespace nitou.BlockPG.Blocks {
    using nitou.BlockPG.Interface;

    /// <summary>
    /// The base class for block instance. Serves as a façade.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public abstract class BPG_BlockBase : BPG_ComponentBase, I_BPG_Block {

        /// <summary>
        /// ブロック分類．
        /// </summary>
        public abstract BlockType Type { get; }

        /// <summary>
        /// ブロックが所属する親セクション．ルートブロックの場合はnullになる．
        /// </summary>
        public I_BPG_BlockSection ParentSection { get; protected set; }

        /// <summary>
        /// 子階層のレイアウト．
        /// </summary>
        public I_BPG_BlockLayout Layout { get; protected set; }

        /// <summary>
        /// ドラッグ操作時の挙動．
        /// </summary>
        public I_BPG_Draggable Drag { get; protected set; }

        /// <summary>
        /// ブロックの機能実装．
        /// </summary>
        public I_BPG_Instruction Instruction { get; protected set; }

        /// <summary>
        /// 識別ID．インスタンスごとに一意．
        /// [NOTE] 以前は gameObject.GetInstanceID() を使っていたが、実行のたびに値が変わるため
        ///        セーブデータに保存しても復元できなかった．GUIDに変更している．
        /// </summary>
        public string Id {
            get {
                if (string.IsNullOrEmpty(_id)) {
                    _id = Guid.NewGuid().ToString("N");
                }
                return _id;
            }
        }

        private string _id;


        /// ----------------------------------------------------------------------------
        // Lifecycle Events

        //private void LateUpdate() {
        //    if(this.IsRootBlock()) {
        //        Layout.UpdateLayout();
        //    }
        //}

        //private void Start() => Debug.Log("[Block] Start");

        protected virtual void Awake() {
            //Debug.Log("[Block] Awake");
            OnInitialize();
        }
        
        protected virtual void OnInitialize() { }


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// Set the active state of the Shadow.
        /// </summary>
        public virtual void SetShadowActive(bool isActive) { }

        /// <summary>
        ///
        /// </summary>
        public void SetParentSection(I_BPG_BlockSection parentSection) {
            ParentSection = parentSection;
        }

        /// <summary>
        /// 識別IDを設定する．（※セーブデータからの復元時に使用する）
        /// </summary>
        public void SetId(string id) {
            if (string.IsNullOrEmpty(id)) {
                Debug.LogWarning("Block id must not be null or empty.");
                return;
            }
            _id = id;
        }


        /// ----------------------------------------------------------------------------
        // Protected Method

        /// <summary>
		/// Obtain a reference to a block-related components.
		/// </summary>
		protected void GatherComponents() {
            Layout = gameObject.GetComponent<I_BPG_BlockLayout>();
            Drag = gameObject.GetComponent<I_BPG_Draggable>();
        }

        /// <summary>
        /// Obtains a reference to the parent section to which it belongs.
        /// </summary>
        protected void GatherParentSection() {
            ParentSection = gameObject.GetComponentInParent<I_BPG_BlockSection>();
        }

        protected void AdjustTransform() {
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            transform.localEulerAngles = Vector3.zero;
        }


        /// ----------------------------------------------------------------------------
#if UNITY_EDITOR
        protected virtual void OnValidate() {
            

        }
#endif
    }
}