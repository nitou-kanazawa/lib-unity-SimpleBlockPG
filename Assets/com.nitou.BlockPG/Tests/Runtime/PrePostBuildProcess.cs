using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

// [REF]
//  LIGHT11: Unity Test Runner（Test Framework）でテストの前後処理を書く方法まとめ https://light11.hatenadiary.com/entry/2020/06/14/190752

namespace RuntimeTests {

    /// <summary>
    /// テスト用のリソース生成・削除を担うクラス．
    /// </summary>
    public class PrePostBuildProcess : IPrebuildSetup, IPostBuildCleanup {

        private static readonly string Path = "Assets/";


        void IPrebuildSetup.Setup() {
#if UNITY_EDITOR

#endif
        }
        
        void IPostBuildCleanup.Cleanup() {
#if UNITY_EDITOR

#endif
        }
    
    }

}