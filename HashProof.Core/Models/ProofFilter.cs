using System;
using System.Collections.Generic;
using MongoDB.Driver.Core.Authentication;

namespace HashProof.Core.Models
{
    public enum SortOrder
    {
        Default = 0,
        Ascending =1,
        Descending = 2
    }
    public class ProofFilter : BaseFilter
    {
        public string Hash { get; set; }

        public string Address { get; set; }

        public bool? PayConfirmed { get; set; } 

        public decimal? PayAmount { get; set; }

        public DateTime? PayDate { get; set; }

        public IEnumerable<string> Statuses { get; set; }

        public SortOrder SortByDate { get; set; }

        public int? Limit { get; set; }
        
        public int? Skip { get; set; } 

        public string Search { get; set; }
    }
}
