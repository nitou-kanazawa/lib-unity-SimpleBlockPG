using System;
using System.Collections.Generic;

namespace Nitou.uBlock {

    public interface ITreeNode<TNode> 
        where TNode: ITreeNode<TNode> {

        /// <summary>
        /// 親ノード．
        /// </summary>
        TNode? Parent { get;}

        /// <summary>
        /// 子ノード．
        /// </summary>
        IEnumerable<TNode> Children { get; }
    }


    public interface IMutableTreenNode<TNode> : ITreeNode<TNode> 
        where TNode: IMutableTreenNode<TNode> {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        TNode AddChild(TNode child);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        TNode InsertChild(int index, TNode child);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        TNode RemoveChild(TNode child);

        /// <summary>
        /// 全ての子要素を取り除く．
        /// </summary>
        /// <returns>取り除かれた子要素</returns>
        IReadOnlyList<TNode> ClearChildren();
    }    
}
