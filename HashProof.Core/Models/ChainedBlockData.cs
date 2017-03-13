using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using nStratis;

namespace HashProof.Core.Models
{
    [DataContract]
    public class ChainedBlockData
    {
        [DataMember(Name = "hash")]
        public string Hash { get; set; }

        [DataMember(Name = "chainwork")]
        public string ChainWork { get; set; }

        [DataMember(Name = "previous")]
        public string Previous { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "header")]
        public BlockHeaderData Header { get; set; }

        public static ChainedBlockData Parse(ChainedBlock block)
        {
            var chainedBlock = new ChainedBlockData
            {
                Hash = block.HashBlock.ToString(),
                ChainWork = block.ChainWork.ToString(),
                Height = block.Height,
                Previous = block.Previous.HashBlock.ToString(),
                Header = BlockHeaderData.Parse(block.Header)
            };

            return chainedBlock;
        }
    }
}
