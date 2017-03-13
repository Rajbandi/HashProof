using System;
using System.IO;
using nStratis;
using Newtonsoft.Json;

namespace HashProof.Core.Converters
{
    /// <summary>
    /// File extracted from https://github.com/LykkeCity/BlockchainExplorer project
    /// 
    /// </summary>
    public class UInt256JsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(uint256) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            try
            {
                return uint256.Parse((string)reader.Value);
            }
            catch (EndOfStreamException)
            {
            }
            catch (FormatException)
            {
            }
            throw new JsonException($"Invalid  object of type {objectType.Name} path {reader.Path}");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}