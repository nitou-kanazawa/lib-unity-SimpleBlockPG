using UniRx;
using UnityEngine;
using UnityEngine.UI;
using nitou.BlockPG.Blocks;
using nitou.BlockPG.Interface;

namespace Develop {

    public class Test_BlockInfo : MonoBehaviour {

        public BPG_BlockBase targetBlock;

        [Header("Prefab")]
        [SerializeField] BPG_BlockBase _movePrefab;
        [SerializeField] BPG_BlockBase _asaePrefab;

        [Space]

        [SerializeField] Button _button1;
        [SerializeField] Button _button2;
        [SerializeField] Button _button3;


        private void Start() {

            _button1.OnClickAsObservable().Subscribe(_ => Add1());

        }

        private void Add1() {
            if (targetBlock == null || !targetBlock.HasParentBlock()) {
                Debug.Log("Target block is null.");
                return;
            }

            var asaeBlock = Instantiate<BPG_BlockBase>(_asaePrefab);
            var moveBlock = Instantiate<BPG_BlockBase>(_movePrefab);

            var body = asaeBlock.GetFirstSection().Body;
            body.AppendLast(moveBlock);


            if (!asaeBlock.ConnectTo(targetBlock)) {
                Destroy(asaeBlock);
            }

        }

        private void Add2() {

        }

        private void Add3() {

        }



        [ContextMenu("Display block info")]
        public void DisplayBlockInfo() {
            if (targetBlock == null) {
                Debug.Log("Target block is null.");
                return;
            }

            // テストコード
            {
                var rootBlock = targetBlock.GetRootBlock();
                Debug.Log($"Root : {rootBlock?.RectTransform.name}");

                var parentBlock = targetBlock.GetParentBlock();
                Debug.Log($"Parent : {parentBlock?.RectTransform.name}");

                var previousBlock = targetBlock.GetPreviousBlock();
                Debug.Log($"Previous : {previousBlock?.RectTransform.name}");

                var nextBlock = targetBlock.GetNextBlock();
                Debug.Log($"Next : {nextBlock?.RectTransform.name}");

            }
        }

        [ContextMenu("Append")]
        public void Append() {
            if (targetBlock == null)
                return;

            var newBlock = Instantiate<BPG_BlockBase>(_movePrefab);
            if (!newBlock.AppendTo(targetBlock)) {
                Destroy(newBlock);
            }
            Debug.Log(newBlock.Layout.Size);

            Observable.NextFrame()
                .Subscribe(_ => Debug.Log(newBlock.Layout.Size))
                .AddTo(this);

        }

        [ContextMenu("InsertNext")]
        public void InsertNext() {
            if (targetBlock == null)
                return;

            var newBlock = Instantiate<BPG_BlockBase>(_movePrefab);
            if (!newBlock.ConnectTo(targetBlock)) {
                Destroy(newBlock);
            }
        }

    }
}
