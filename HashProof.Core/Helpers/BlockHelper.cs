using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using nStratis;
using Newtonsoft.Json;

namespace HashProof.Core.Helpers
{
    public static class BlockHelper
    {
        public static string ToJson(Block block, ChainedBlock chainedBlock=null)
        {
            var strWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(strWriter)
            {
                Formatting = Formatting.Indented,
            };
            jsonWriter.WriteStartObject();
            ToJson(jsonWriter, block, chainedBlock);
            jsonWriter.WriteEndObject();
            jsonWriter.Flush();
            return strWriter.ToString();
        }

        public static void ToJson(JsonTextWriter writer, Block block, ChainedBlock chainedBlock = null)
        {
            JsonHelper.WritePropertyValue(writer, "hash", block.GetHash().ToString());
            if (chainedBlock != null)
            {
                JsonHelper.WritePropertyValue(writer, "height", chainedBlock.Height);
                JsonHelper.WritePropertyValue(writer, "chainwork", chainedBlock.ChainWork.ToString());
            }
            JsonHelper.WritePropertyValue(writer, "proofofstake", block.IsProofOfStake());
            JsonHelper.WritePropertyValue(writer, "proofofwork", block.IsProofOfWork());
            JsonHelper.WritePropertyValue(writer, "entropybit", block.GetStakeEntropyBit());
            //JsonHelper.WritePropertyValue(writer, "length", block.Length);
            

            //Header 
            var header = block.Header;
           // writer.WritePropertyName("header");
            //writer.WriteStartObject();
            //JsonHelper.WritePropertyValue(writer, "hash", block.Header.GetHash().ToString());
            JsonHelper.WritePropertyValue(writer, "powhash", block.Header.GetPoWHash().ToString());
            JsonHelper.WritePropertyValue(writer, "version", block.Header.Version);
            JsonHelper.WritePropertyValue(writer, "bits", block.Header.Bits.ToUInt256().ToString());
            JsonHelper.WritePropertyValue(writer, "difficulty", block.Header.Bits.Difficulty);
            JsonHelper.WritePropertyValue(writer, "blocktime", block.Header.BlockTime.ToString());
            JsonHelper.WritePropertyValue(writer, "merkleroot", block.Header.HashMerkleRoot.ToString());
            JsonHelper.WritePropertyValue(writer, "hashprevblock", block.Header.HashPrevBlock.ToString());
            JsonHelper.WritePropertyValue(writer, "nonce", block.Header.Nonce);
            JsonHelper.WritePropertyValue(writer, "time", block.Header.Time);

            var posParameters = block.Header.PosParameters;
            JsonHelper.WritePropertyValue(writer, "flags", posParameters.Flags.ToString());
            JsonHelper.WritePropertyValue(writer, "mint",posParameters.Mint);
            JsonHelper.WritePropertyValue(writer, "proofhash", posParameters.HashProof.ToString());
            JsonHelper.WritePropertyValue(writer, "modifier", posParameters.StakeModifier.ToString());
            JsonHelper.WritePropertyValue(writer, "modifierv2", posParameters.StakeModifierV2.ToString());
            JsonHelper.WritePropertyValue(writer, "staketime", posParameters.StakeTime);

            //writer.WriteEndObject();



            //Transactions 

            var transactions = block.Transactions;
            writer.WritePropertyName("transactions");
            writer.WriteStartArray();
            foreach (var tx in transactions)
            {
                writer.WriteStartObject();
                TransactionHelper.ToJson(writer, tx);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();


            JsonHelper.WritePropertyValue(writer, "signature", block.BlockSignatur.ToString());

        }

        public static Block FromJson(string json)
        {
            Block block = null;
            var strReader = new StringReader(json);
            var reader = new JsonTextReader(strReader);
            
            return block;
        }


    }
}
