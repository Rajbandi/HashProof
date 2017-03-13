
using System;
using System.IO;
using System.Reflection;
using nStratis;
using Newtonsoft.Json;

namespace HashProof.Core.Converters
{
    /// <summary>
    /// File extracted from https://github.com/LykkeCity/BlockchainExplorer project
    /// 
    /// </summary>
    public class LockTimeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
           return typeof(LockTime).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            try
            {
                return new LockTime((int)reader.Value);
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
            writer.WriteValue(((LockTime)value).Value.ToString());
        }
    }
}