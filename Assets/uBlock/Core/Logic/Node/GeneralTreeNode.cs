using System;
using System.Collections.Generic;

namespace Nitou.uBlock {

    [Serializable]
    public class GeneralTreeNode<TNode> : TreeNodeBase<TNode>
        where TNode : GeneralTreeNode<TNode> {


        protected List<TNode> _children;

        public override IEnumerable<TNode> Children => throw new NotImplementedException();


        /// ----------------------------------------------------------------------------
        // Public Method


        /// ----------------------------------------------------------------------------
        // Protected Method

        protected void ThrowIfDisposed() {

        }


        /// ----------------------------------------------------------------------------
        // Private Method

    }
}
