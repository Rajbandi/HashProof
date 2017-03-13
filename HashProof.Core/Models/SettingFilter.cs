using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HashProof.Core.Models
{
    public class SettingFilter : BaseFilter
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
