using System.Collections.Generic;

namespace HashProof.Core.Models
{
    public class BlockFilter : BaseFilter
    {
        public int? BlockHeight { get; set; }
        public string BlockId { get; set; }
        public string BlockSignature { get; set; }
        public string TxId { get; set; }
        public string SenderAddress { get; set; }
        public string SenderPubKey { get; set; }

        public string ReceiverAddress { get; set; }
        public string ReceiverPubKey { get; set; }
        public string ScriptPubKey { get; set; }

        public IEnumerable<string> UnSpents { get; set; }
    }
}
