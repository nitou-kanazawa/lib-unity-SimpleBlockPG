using System;
using System.Collections.Generic;
using System.Linq;

namespace Nitou.uBlock {

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="node"></param>
    /// <returns></returns>
    public delegate IEnumerable<T?> GetNodesDelegate<T>(T node) where T : ITreeNode<T>;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">ノードの型．</typeparam>
    /// <param name="currentNode">現在のノード．</param>
    /// <param name="newNodes">追加されたノード．</param>
    /// <param name="remainingNodes">未処理のノード．</param>
    /// <returns></returns>
    public delegate IEnumerable<T?> UpdatePendingNodesDelegate<T>(
        T currentNode, IEnumerable<T?> newNodes, IEnumerable<T?> remainingNodes
    ) where T : ITreeNode<T>;


    /// <summary>
    /// <see cref="ITreeNode{TNode}"/> 型の拡張メソッドを提供する．
    /// </summary>
    public static partial class TreeNodeExtensions {

        /// ----------------------------------------------------------------------------
        #region 要素の列挙

        /// <summary>
		/// Expands and enumerates a tree structure starting from the current node, allowing custom logic for adding and updating nodes during the traversal.
		/// </summary>
		/// <typeparam name="T">ノードの型．</typeparam>
		/// <param name="startNode">開始ノード．</param>
		/// <param name="getNodes">
		/// A function that determines additional nodes to add based on the specified node.  
		/// Takes the current node as an argument and returns a collection of nodes related to it.
		/// </param>
		/// <param name="updatePendingNodes">
		/// A function that updates the list of unenumerated nodes during traversal. This function takes the following arguments:
		/// <list type="bullet">
		/// <item><description>現在のノード． The value passed as the first argument to the <paramref name="getNodes"/> function.</description></item>
		/// <item><description>追加されたノード． The return value of the <paramref name="getNodes"/> function.</description></item>
		/// <item><description>未処理のノード．
		/// <para>If the head element of this collection has not been used as an argument to <paramref name="getNodes"/>, it will be passed as the argument to <paramref name="getNodes"/>. </para>
        /// <para>If it has already been used as an argument to <paramref name="getNodes"/>, that element will be enumerated.</para>
		/// </description></item>
		/// </list>
		/// </param>
		public static IEnumerable<T> Evolve<T>(this ITreeNode<T> startNode,
            GetNodesDelegate<T> getNodes,
            UpdatePendingNodesDelegate<T> updatePendingNodes
        ) where T : ITreeNode<T> {
            ISet<T> exphistory = new HashSet<T>();  //展開した履歴
            ISet<T> rtnhistory = new HashSet<T>();  //列挙した履歴
            IEnumerable<T?> seeds = new T[1] { (T)startNode };
            while (expand(ref exphistory, out T? cur, ref seeds, getNodes, updatePendingNodes)) {
                if (cur != null && rtnhistory.Add(cur)) yield return cur;
            }
        }

        /// <summary>
        /// ツリーの探索を制御するメソッド．
        /// </summary>
        private static bool expand<T>(ref ISet<T> history, out T? cur, ref IEnumerable<T?> seeds,
            GetNodesDelegate<T> getNewSeeds,
            UpdatePendingNodesDelegate<T> updateseeds
        ) where T : ITreeNode<T> {
            if (!seeds.Any()) { cur = default; return false; }
            cur = seeds.First();
            while (cur != null && history.Add(cur)) {
                var newSeeds = getNewSeeds(cur);
                seeds = updateseeds(cur, newSeeds, seeds.Skip(1));
                cur = seeds.FirstOrDefault();
            }
            seeds = seeds.Skip(1);
            return true;
        }


        #endregion

    }
}
