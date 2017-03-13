using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using nStratis;

namespace HashProof.Core.Models
{
    [DataContract]
    public class SavedOutPoint
    {
        [DataMember]
        public string Hash { get; set; }

        [DataMember]
        public uint N { get; set; }

        public static SavedOutPoint FromOutPoint(OutPoint pos)
        {
            var outPoint = new SavedOutPoint
            {
                Hash = pos.Hash.ToString(),
                N = pos.N
            };

            return outPoint;
        }
    }

    [DataContract]
    public class SavedPosParameters
    {
        [DataMember]
        public string HashProof { get; set; }

        [DataMember]
        public int Mint { get; set; }
        public int Flags { get; set; }
        public ulong StakeModifier { get; set; }
        public uint StakeTime { get; set; }
        public string StakeModifierV2 { get; set; }
        public SavedOutPoint PrevoutStake { get; set; }
        public static SavedPosParameters FromPosParameters(PosParameters pos)
        {
            var posParams = new SavedPosParameters
            {
                HashProof = pos.HashProof.ToString(),
                Mint = pos.Mint,
                Flags = (int)pos.Flags,
                StakeModifier = pos.StakeModifier,
                StakeModifierV2 = pos.StakeModifierV2.ToString(),
                StakeTime = pos.StakeTime,
                PrevoutStake = SavedOutPoint.FromOutPoint(pos.PrevoutStake)
            };
            return posParams;
        }
    }

    public class SavedBlockHeader
    {
        public double Bits { get; set; }
        public long BlockTime { get; set; }
        public string HashPrevBlock { get; set; }
        public string HashMerkleRoot { get; set; }
        public uint Time { get; set; }
        public uint Nonce { get; set; }
        public int Version { get; set; }

        public SavedPosParameters PosParameters { get; set; }

        public static SavedBlockHeader FromBlockHeader(BlockHeader header)
        {
            var savedHeader = new SavedBlockHeader
            {
                Bits = header.Bits.Difficulty,
                BlockTime = header.BlockTime.ToUnixTimeSeconds(),
                Time = header.Time,
                Nonce = header.Nonce,
                Version = header.Version,
                HashMerkleRoot = header.HashMerkleRoot.ToString(),
                HashPrevBlock = header.HashPrevBlock.ToString(),
                PosParameters = SavedPosParameters.FromPosParameters(header.PosParameters)
            };
            return savedHeader;
        }
    }

    public class SavedTxIn
    {
        public SavedOutPoint PrevOut { get; set; }
        public bool IsFinal { get; private set; }
        public string ScriptSig { get; set; }
        public string WitScript { get; set; }
        public uint Sequence { get; set; }

        public static SavedTxIn FromTxIn(TxIn tx)
        {
            return new SavedTxIn
            {
                Sequence = tx.Sequence,
                ScriptSig = tx.ScriptSig.ToString(),
                WitScript = tx.WitScript.ToString(),
                IsFinal = tx.IsFinal,
                PrevOut = SavedOutPoint.FromOutPoint(tx.PrevOut)
            };
        }
    }

    public class SavedTxOut
    {
        public string ScriptPubKey { get; set; }
        public decimal Value { get; set; }
        public string Id { get; set; }
        public bool IsEmpty { get; private set; }
        public bool IsNull { get; private set; }
       
        public static SavedTxOut FromTxOut(TxOut tx)
        {
            return new SavedTxOut
            {
                ScriptPubKey = tx.ScriptPubKey.ToString(),
                Value = Money.Satoshis(tx.Value).ToUnit(MoneyUnit.BTC),
                IsEmpty= tx.IsEmpty,
                IsNull = tx.IsNull
            };
        }
    }

    public class SavedTransaction
    {
        public bool HasWitness { get; set; }
        public bool IsCoinBase { get; private set; }
        public bool IsCoinStake { get; private set; }
        public bool RBF { get; set; }

        public uint LockTime { get; set; }
        public uint Time { get; set; }
        public uint Version { get; set; }

        public long TotalOut { get; set; }

        public List<SavedTxOut> Outputs = new List<SavedTxOut>();
        public List<SavedTxIn> Inputs = new List<SavedTxIn>();

        public static SavedTransaction FromTransaction(Transaction tx)
        {
            var transaction = new SavedTransaction();

            transaction.HasWitness = tx.HasWitness;
            transaction.IsCoinStake = tx.IsCoinStake;
            transaction.IsCoinBase = tx.IsCoinBase;
            transaction.LockTime = tx.LockTime.Value;
            transaction.RBF = tx.RBF;
            transaction.Time = tx.Time;
            transaction.TotalOut = tx.TotalOut;
            transaction.Version = tx.Version;

            var inputs = transaction.Inputs;
            foreach (var input in tx.Inputs)
            {
                inputs.Add(SavedTxIn.FromTxIn(input));
            }


            var outputs = transaction.Outputs;
            foreach (var output in tx.Outputs)
            {
                outputs.Add(SavedTxOut.FromTxOut(output));
            }

            return transaction;
        }
    }

    public class SavedBlock
    {
        public string Hash { get; set; }
        public string MerkleRoot { get; set; }
        public bool HeaderOnly { get; set; }
        public uint Height { get; set; }
        public string BlockSignature { get; set; }
        public bool IsProofOfStake { get; private set; }
        public bool IsProofOfWork { get; private set; }
        public int Length { get; private set; }

        public SavedBlockHeader Header { get; set; }

        public List<SavedTransaction> Transactions = new List<SavedTransaction>();

        public static SavedBlock FromBlock(Block b)
        {
            var bl = new SavedBlock
            {
                Hash = b.GetHash().ToString(),
                HeaderOnly = b.HeaderOnly,
                Header = SavedBlockHeader.FromBlockHeader(b.Header),

            };
            var bstr = b.ToString();

            var transactions = bl.Transactions;
            foreach (var tx in b.Transactions)
            {
                var str = tx.ToString();
                var str1 = tx.ToString(RawFormat.BlockExplorer);
                var str2 = tx.ToString(RawFormat.Satoshi);

                transactions.Add(SavedTransaction.FromTransaction(tx));
            }

            bl.BlockSignature = b.BlockSignatur.ToString();
            bl.Hash = b.GetHash().ToString();
            bl.MerkleRoot = b.GetMerkleRoot().ToString();
            bl.IsProofOfStake = b.IsProofOfStake();
            bl.IsProofOfWork = b.IsProofOfWork();
            bl.Length = b.Length;
            return bl;
        }
    }


}

