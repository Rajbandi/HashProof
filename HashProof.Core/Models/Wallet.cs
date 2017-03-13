using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HashProof.Core.Models
{
    [DataContract]
    public class Wallet
    {
        [DataMember]
        public List<WalletAddress> Addresses = new List<WalletAddress>();

        public Wallet(string masterKey)
        {
          
        }
    }
}
