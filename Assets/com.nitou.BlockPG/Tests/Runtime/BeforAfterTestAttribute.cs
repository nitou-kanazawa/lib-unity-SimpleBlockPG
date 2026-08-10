using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestTools;

namespace RuntimeTests
{
    public class BeforeAfterTestAttribute : NUnitAttribute, 
        IOuterUnityTestAction {

        // テスト前処理
        public IEnumerator BeforeTest(ITest test) {
            Debug.Log("Before Test");
            yield break;
        }

        // テスト後処理
        public IEnumerator AfterTest(ITest test) {
            Debug.Log("After Test");
            yield break;
        }
    }
}
