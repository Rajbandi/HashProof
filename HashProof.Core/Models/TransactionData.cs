using System.Collections.Generic;
using System.Runtime.Serialization;
using nStratis;
using nStratis.DataEncoders;

namespace HashProof.Core.Models
{
    [DataContract]
    public class TransactionData
    {
        [DataMember(Name = "hash")]
        public string Hash { get; set; }

        [DataMember(Name = "ver")]
        public long Version { get; set; }

        [DataMember(Name="vin_sz")]
        public int InputCount { get; set; } 

        [DataMember(Name="vout_sz")]
        public int OutputCount { get; set; }

        [DataMember(Name = "time")]
        public long Time { get; set; }

        [DataMember(Name = "lock_time")]
        public long LockTime { get; set; }

        [DataMember(Name = "size")]
        public int Size { get; set; }

        [DataMember(Name="in")]
        public List<TransactionInput> Inputs { get; set; }

        [DataMember(Name="out")]
        public List<TransactionOutput> Outputs { get; set; }

        public IEnumerable<TransactionOutput> IndexedOutputs
        {
            get
            {
                var index = 0;
                foreach (var output in Outputs)
                {
                    output.Index = index++;
                    yield return output;
                }
            }
        }

        public static TransactionData Parse(Transaction tx)
        {
            var txData = new TransactionData
            {
                Hash = tx.GetHash().ToString(),
                Version = tx.Version,
                InputCount = tx.Inputs.Count,
                OutputCount = tx.Outputs.Count,
                Time = tx.Time,
                LockTime = tx.LockTime.Value,
                Size = tx.GetSerializedSize()
            };

            var inputs = new List<TransactionInput>();
            foreach (var input in tx.Inputs.AsIndexedInputs())
            {
                inputs.Add(TransactionInput.Parse(input.TxIn));
            }
            txData.Inputs = inputs;

            var outputs = new List<TransactionOutput>();
            var index = 0;
            foreach (var output in tx.Outputs.AsIndexedOutputs())
            {
                var txOut = TransactionOutput.Parse(output.TxOut);
                txOut.Index = index++;
                outputs.Add(txOut);
            }
            txData.Outputs = outputs;

            return txData;
        }
    }

    [DataContract]
    public class TransactionInput
    {
        [DataMember(Name = "prev_out")]
        public TransactionOutPoint PrevOut { get; set; }

        [DataMember(Name = "coinbase")]
        public string CoinBase { get; set; }

        [DataMember(Name = "scriptSig")]
        public string ScriptSig { get; set; }

        [DataMember(Name = "witness")]
        public string WitScript { get; set; }

        [DataMember(Name = "sequence")]
        public long Sequence { get; set; }

        public static TransactionInput Parse(TxIn txin)
        {
            var tx = new TransactionInput();
            tx.CoinBase = txin.PrevOut.Hash == uint256.Zero ? Encoders.Hex.EncodeData(txin.ScriptSig.ToBytes()) : "";
            tx.ScriptSig = txin.PrevOut.Hash != uint256.Zero ? txin.ScriptSig.ToString() : "";

            tx.WitScript = txin.WitScript != nStratis.WitScript.Empty ? txin.WitScript.ToString() : "";
            tx.Sequence = txin.Sequence != uint.MaxValue ? (long)txin.Sequence : default(long);

            tx.PrevOut = TransactionOutPoint.Parse(txin.PrevOut);

            return tx;
        }


        public bool IsFrom(string pubKey)
        {

            var sig = new Script(ScriptSig);
            var result = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(sig);
            return result != null && result.PublicKey.ToString() == pubKey;
        }

    }

    [DataContract]
    public class TransactionOutput
    {
        [DataMember(Name = "scriptPubKey")]
        public string ScriptPubKey { get; set; }

        [DataMember(Name = "value")]
        public decimal Value { get; set; }

        [DataMember(Name = "index")]
        public int Index { get; set; }

        public static TransactionOutput Parse(TxOut txout)
        {
            var tx = new TransactionOutput
            {
                ScriptPubKey = txout.ScriptPubKey.ToString(),
                Value = txout.Value.ToDecimal(MoneyUnit.BTC),
                
            };

            return tx;
        }
    }

    [DataContract]
    public class TransactionOutPoint
    {
        [DataMember(Name = "hash")]
       public string Hash { get; set; }

        [DataMember(Name = "n")]
        public long N { get; set; }

        public static TransactionOutPoint Parse(OutPoint outpoint)
        {
            return new TransactionOutPoint
            {
                Hash = outpoint.Hash.ToString(),
                N = outpoint.N
            };
        }
    }
}
