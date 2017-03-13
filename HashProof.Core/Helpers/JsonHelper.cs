using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using nStratis;
using nStratis.DataEncoders;
using Newtonsoft.Json;

namespace HashProof.Core.Helpers
{
    public static class JsonHelper
    {
       

       public  static void WritePropertyValue<TValue>(JsonWriter writer, string name, TValue value)
        {
            writer.WritePropertyName(name);
            writer.WriteValue(value);
        }

    }
}
