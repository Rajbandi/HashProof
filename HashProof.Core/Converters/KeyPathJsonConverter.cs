using System;
using System.Reflection;
using nStratis.BIP32;
using Newtonsoft.Json;

namespace HashProof.Core.Converters
{
    /// <summary>
    /// File extracted from https://github.com/LykkeCity/BlockchainExplorer project
    /// 
    /// </summary>
    public class KeyPathJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(KeyPath).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                return reader.TokenType == JsonToken.Null ? null : KeyPath.Parse(reader.Value.ToString());
            }
            catch (FormatException)
            {
                throw new JsonException($"Invalid  object of type {objectType.Name} path {reader.Path}");
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var keyPath = value as KeyPath;
            if (keyPath != null)
                writer.WriteValue(keyPath.ToString());
        }
    }
}