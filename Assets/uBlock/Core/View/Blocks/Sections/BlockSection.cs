using UnityEngine;

namespace Nitou.uBlock {

    [DisallowMultipleComponent]
    public sealed class BlockSection : ComponentBase {

        [SerializeField] BlockSectionHeader _header;
        [SerializeField] BlockSectionBody _body;


        public BlockSectionHeader Header => _header;

        public BlockSectionBody Body => _body;
    }


    public static class BlockSectionExtensions {

        public static bool HasBody(this BlockSection section) => section.Body != null;

    }
}
