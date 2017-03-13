using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using nStratis;
using nStratis.BIP32;

namespace HashProof.Core.Models
{
    [DataContract]
    public class WalletAddress
    {
        [DataMember]
        public string KeyPath { get; set; }
        [DataMember]
        public string PubKey { get; set; }
        [DataMember]
        public string Address { get; set; }

        protected WalletAddress()
        {
            
        }

        public WalletAddress(string address, string pubKey, string path)
        {
            Address = address;
            PubKey = pubKey;
            KeyPath = path;
            
        }

    }
}
