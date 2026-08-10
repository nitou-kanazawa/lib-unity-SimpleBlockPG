using UnityEngine;
using UniRx;

namespace State {

    public class StateManager : MonoBehaviour {
        public enum State {
            Teaching,
            Playing
        }

        // ReactivePropertyでStateを管理
        public ReactiveProperty<State> CurrentState { get; } = new (State.Teaching);
    
    }




}