using System.Collections.Generic;
using System.Runtime.Serialization;
using nStratis;

namespace HashProof.Core.Models
{
    [DataContract]
    public class BlockData
    {
        [DataMember(Name = "hash")]
        public string Hash { get; set; }

        [DataMember(Name = "chainwork")]
        public string ChainWork { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "proofofstake")]
        public bool IsProofOfStake { get; set; }

        [DataMember(Name = "proofofwork")]
        public bool IsProofOfWork { get; set; }

        [DataMember(Name = "entropybit")]
        public ulong StakeEntropyBit { get; set; }

        [DataMember(Name = "header")]
        public BlockHeaderData Header { get; set; }

        [DataMember(Name = "transactions")]
        public List<TransactionData> Transactions { get; set; }

        [DataMember(Name = "signature")]
        public string Signature{ get; set; }

        public static BlockData Parse(Block block, ChainedBlock chainedBlock = null)
        {
            var blockData = new BlockData {Hash = block.GetHash().ToString()};
            if (chainedBlock != null)
            {
                blockData.Height = chainedBlock.Height;
                blockData.ChainWork = chainedBlock.ChainWork.ToString();
            }
            blockData.IsProofOfStake = block.IsProofOfStake();
            blockData.IsProofOfWork = block.IsProofOfWork();
            blockData.StakeEntropyBit = block.GetStakeEntropyBit();
        
            //Header
            blockData.Header = BlockHeaderData.Parse(block.Header);

            //Transactions
            var transactions = new List<TransactionData>();
            foreach (var tx in block.Transactions)
            {
                transactions.Add(TransactionData.Parse(tx));
            }
            blockData.Transactions = transactions;

            blockData.Signature = block.BlockSignatur.ToString();
            return blockData;
        }

        
    }

    [DataContract]
    public class BlockHeaderData
    {

        [DataMember(Name = "merkleroot")]
        public string MerkleRoot { get; set; }

        [DataMember(Name = "hashprevblock")]
        public string HashPrevBlock { get; set; }

        [DataMember(Name = "powhash")]
        public string PowHash { get; set; }

        [DataMember(Name = "version")]
        public int Version { get; set; }

        [DataMember(Name = "bits")]
        public string Bits { get; set; }

        [DataMember(Name = "difficulty")]
        public double Difficulty { get; set; }

        [DataMember(Name = "blocktime")]
        public long BlockTime { get; set; }

        [DataMember(Name = "time")]
        public long Time { get; set; }

        [DataMember(Name = "nonce")]
        public long Nonce { get; set; }
        [DataMember(Name = "posdata")]
        public BlockPosData PosData { get; set; }

        public static BlockHeaderData Parse(BlockHeader header)
        {
            var headerData = new BlockHeaderData
            {
                PowHash = header.GetPoWHash().ToString(),
                Version = header.Version,
                Bits = header.Bits.ToUInt256().ToString(),
                Difficulty = header.Bits.Difficulty,
                BlockTime = header.BlockTime.ToUnixTimeSeconds(),
                MerkleRoot = header.HashMerkleRoot.ToString(),
                HashPrevBlock = header.HashPrevBlock.ToString(),
                Nonce = header.Nonce,
                Time = header.Time,
                PosData = BlockPosData.Parse(header.PosParameters)
            };
            
            return headerData;
            
        }
    }

    [DataContract]
    public class BlockPosData
    {
        [DataMember(Name = "flags")]
        public string Flags { get; set; }

        [DataMember(Name = "mint")]
        public int Mint { get; set; }

        [DataMember(Name = "proofhash")]
        public string ProofHash { get; set; }

        [DataMember(Name = "modifier")]
        public string StakeModifier { get; set; }

        [DataMember(Name = "modifierv2")]
        public string StakeModifierV2 { get; set; }

        [DataMember(Name = "staketime")]
        public uint StakeTime { get; set; }

        public static BlockPosData Parse(PosParameters pos)
        {
            var posData = new BlockPosData
            {
                Flags = pos.Flags.ToString(),
                Mint = pos.Mint,
                ProofHash = pos.HashProof.ToString(),
                StakeModifier = pos.StakeModifier.ToString(),
                StakeModifierV2 = pos.StakeModifierV2.ToString(),
                StakeTime = pos.StakeTime
            };


            return posData;
        }
    }

}
