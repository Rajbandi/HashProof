using nStratis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using nStratis.DataEncoders;
using Newtonsoft.Json;

namespace HashProof.Core.Helpers
{
    public static class TransactionHelper
    {
        public static string ToJson(Transaction tx)
        {
            var strWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(strWriter);
            jsonWriter.Formatting = Formatting.Indented;
            jsonWriter.WriteStartObject();
            ToJson(jsonWriter, tx);
            jsonWriter.WriteEndObject();
            jsonWriter.Flush();
            return strWriter.ToString();
        }

        public static void ToJson(JsonTextWriter writer, Transaction tx)
        {
            JsonHelper.WritePropertyValue(writer, "hash", tx.GetHash().ToString());
            JsonHelper.WritePropertyValue(writer, "ver", tx.Version);

            JsonHelper.WritePropertyValue(writer, "vin_sz", tx.Inputs.Count);
            JsonHelper.WritePropertyValue(writer, "vout_sz", tx.Outputs.Count);

            JsonHelper.WritePropertyValue(writer, "time", tx.Time);
            JsonHelper.WritePropertyValue(writer, "lock_time", tx.LockTime.Value);

            JsonHelper.WritePropertyValue(writer, "size", tx.GetSerializedSize());

            writer.WritePropertyName("in");
            writer.WriteStartArray();
            foreach (var input in tx.Inputs.AsIndexedInputs())
            {
                var txin = input.TxIn;
                writer.WriteStartObject();
                writer.WritePropertyName("prev_out");
                writer.WriteStartObject();
                JsonHelper.WritePropertyValue(writer, "hash", txin.PrevOut.Hash.ToString());
                JsonHelper.WritePropertyValue(writer, "n", txin.PrevOut.N);
                writer.WriteEndObject();

                if (txin.PrevOut.Hash == uint256.Zero)
                {
                    JsonHelper.WritePropertyValue(writer, "coinbase", Encoders.Hex.EncodeData(txin.ScriptSig.ToBytes()));
                }
                else
                {
                    JsonHelper.WritePropertyValue(writer, "scriptSig", txin.ScriptSig.ToString());
                }
                if (input.WitScript != WitScript.Empty)
                {
                    JsonHelper.WritePropertyValue(writer, "witness", input.WitScript.ToString());
                }
                if (txin.Sequence != uint.MaxValue)
                {
                    JsonHelper.WritePropertyValue(writer, "sequence", (uint)txin.Sequence);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WritePropertyName("out");
            writer.WriteStartArray();

            foreach (var txout in tx.Outputs)
            {
                writer.WriteStartObject();
                JsonHelper.WritePropertyValue(writer, "value", txout.Value.ToString(false, false));
                JsonHelper.WritePropertyValue(writer, "scriptPubKey", txout.ScriptPubKey.ToString());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public static BitcoinAddress GetAddress(string scriptStr)
        {
            return GetAddress(new Script(scriptStr));
        }
        public static BitcoinAddress GetAddress(Script script)
        {
            if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(script))
            {
                return PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(script).GetAddress(Network.Main);
            }
            else
            if (PayToPubkeyTemplate.Instance.CheckScriptPubKey(script))
            {
                return PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(script).GetAddress(Network.Main);
            }
            else
            if (PayToScriptHashTemplate.Instance.CheckScriptPubKey(script))
            {
                return PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(script).GetAddress(Network.Main);
            }
            else
            if (PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(script))
            {
                return PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(script).GetAddress(Network.Main);
            }
            else
            if (PayToWitPubKeyHashTemplate.Instance.CheckScriptPubKey(script))
            {
                return PayToWitPubKeyHashTemplate.Instance.ExtractScriptPubKeyParameters(script).GetAddress(Network.Main);
            }
            return null;
        }

        public static bool IsFrom(string pubKey, string scriptSig)
        {
            var sig = new Script(scriptSig);
            var result = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(sig);
            return result != null && result.PublicKey.ToString() == pubKey;
        }
        public static string GetPubKey(string addr)
        {
            var address = new BitcoinPubKeyAddress(addr, Network.Main);
            if (PayToPubkeyTemplate.Instance.CheckScriptPubKey(address.ScriptPubKey))
                return PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(address.ScriptPubKey).ToString();
            else
                if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(address.ScriptPubKey))
                return PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(address.ScriptPubKey).ToString();

            return null;
        }

    }
}
