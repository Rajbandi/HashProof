using System;
using System.Runtime.Serialization;

namespace HashProof.Core.Models
{
    [DataContract]
    public class Proof
    {
        [DataMember]
        public string Hash { get; set; }

        [DataMember]
        public WalletAddress PayAddress { get; set; }

        [DataMember]
        public decimal PayAmount { get; set; }

        [DataMember]
        public bool PayConfirmed { get; set; }

        [DataMember]
        public DateTime? PayDate { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public string TxId { get; set; }

        [DataMember]
        public string BlockId { get; set; }

        [DataMember]
        public DateTime DateCreated { get; set; }

        [DataMember]
        public DateTime? DateModified { get; set; }

        [DataMember]
        public long BlockHeight { get; set; }
    }
}
