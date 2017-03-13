using System;

namespace HashProof.Core.Converters
{
    /// <summary>
    /// File extracted from https://github.com/LykkeCity/BlockchainExplorer project
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class EnumTypeAttribute : Attribute
    {
        public EnumTypeAttribute(object value, Type type)
        {
            Value = value;
            Type = type;
        }
        public object Value
        {
            get;
            set;
        }
        public Type Type
        {
            get;
            set;
        }
    }
}