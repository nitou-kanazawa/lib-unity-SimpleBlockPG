using System;

namespace Nitou.uBlock {

    [Serializable]
    public record BlockId {

        public Guid Value { get; }

        public BlockId(Guid value) {
            Value = value;
        }
    }
}
