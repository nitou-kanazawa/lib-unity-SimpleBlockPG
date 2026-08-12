using System.Linq;
using UnityEngine;

// [参考]
//  qiita: シングルトンを使ってみよう　https://qiita.com/Teach/items/c146c7939db7acbd7eee
//  kanのメモ帳: MonoBehaviourを継承したシングルトン https://kan-kikuchi.hatenablog.com/entry/SingletonMonoBehaviour

namespace nitou.AssetLoader {

    /// <summary>
    /// MonoBehaviourを継承したシングルトン
    /// ※DontDestroyOnLoadは呼ばない（派生クラス側で行う）
    /// </summary>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour 
        where T : MonoBehaviour {

        private static T _instance;
        public static T Instance {
            get {
                if (_instance == null) {
#if UNITY_2022_1_OR_NEWER
                    _instance = FindAnyObjectByType<T>(); 
#else
                    _instance = FindObjectOfType<T>(includeInactive: true); 
#endif

                    // シーン内に存在しない場合はエラー
                    if (_instance == null) {
                        Debug.LogWarning(typeof(T) + " をアタッチしているGameObjectはありません");
                    }
                }
                return _instance;
            }
        }


        /// ----------------------------------------------------------------------------
        // MonoBehaviour Method

        protected virtual void Awake() {
            // 既にインスタンスが存在するなら破棄
            if (!CheckInstance())
                Destroy(this.gameObject);
        }

        /// <summary>
        /// [NOTE] 静的フィールドの解放．
        ///        "Enter Play Mode Options" でドメインリロードを無効にした場合、
        ///        破棄済みインスタンスへの参照が次の実行に持ち越されるため明示的にクリアする．
        ///        ※開いたジェネリック型では RuntimeInitializeOnLoadMethod が使えないため破棄時に行う．
        /// </summary>
        protected virtual void OnDestroy() {
            if (ReferenceEquals(_instance, this)) {
                _instance = null;
            }
        }


        /// ----------------------------------------------------------------------------
        // Private Method

        /// <summary>
        /// 他のゲームオブジェクトにアタッチされているか調べる
        /// </summary>
        protected bool CheckInstance() {
            // 存在しない（or自分自身）場合
            if (_instance == null) {
                _instance = this as T;
                return true;
            } else if (Instance == this) {
                return true;
            }
            // 既に存在する場合
            return false;
        }
    }
}