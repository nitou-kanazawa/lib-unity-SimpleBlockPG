using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace State {

    public class AsyncProcessor : MonoBehaviour {

        private IDisposable _stateSubscription;
        private CancellationTokenSource _cts;

        [SerializeField] StateManager _stateManager;



        private void Start() {

            _stateSubscription = _stateManager.CurrentState
                .Skip(1)
                .Where(mode => mode is not StateManager.State.Playing)
                .Subscribe();

        }

        private void OnDestroy() {
            _stateSubscription?.Dispose();
            StopAsyncProcess();
        }





        private void OnExitPlayingMode() {



        }

        private async UniTask StartAsyncProcess() {
            Debug.Log("Starting async process...");
            
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try {
                while (!token.IsCancellationRequested) {
                    // 何らかの非同期処理を実行
                    await UniTask.Delay(1000, cancellationToken: token);
                    Debug.Log("Async process running...");
                }
            } catch (OperationCanceledException) {
                Debug.Log("Async process canceled.");
            } finally {
                Debug.Log("Async process ended.");
            }
        }

        private void StopAsyncProcess() {
            if (_cts != null && !_cts.IsCancellationRequested) {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = null;
        }





    }

}