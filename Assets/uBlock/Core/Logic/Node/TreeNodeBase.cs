using System;
using System.Collections.Generic;

namespace Nitou.uBlock {
    
    [Serializable]
    public abstract class TreeNodeBase<TNode> : ITreeNode<TNode>
        where TNode : TreeNodeBase<TNode>, ITreeNode<TNode> {


        public TNode Self => this as TNode;

        /// <summary>
        /// 親ノード．
        /// </summary>
        public TNode? Parent { get; private set; }

        /// <summary>
        /// 子ノード．
        /// </summary>
        public abstract IEnumerable<TNode> Children { get; }


        /// ----------------------------------------------------------------------------
        // Public Method

        public TreeNodeBase() {

        }



        /// ----------------------------------------------------------------------------
        // Protected Method

        protected virtual bool CanAddChildNode(TNode child) {
            if (child == null) return true;


            throw new NotImplementedException();
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// Called by the parent node during processes like addition or removal.
        /// </summary>
        /// <param name="newParent">新しい親ノード</param>
        /// <returns>True if the parent node is successfully set; otherwise, false.</returns>
        private bool SetParent(TNode? newParent) {
            if (this.Parent == newParent) return false;
            this.Parent = newParent;
            return true;
        }
    }
}
