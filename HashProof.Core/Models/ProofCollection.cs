using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HashProof.Core.Models
{
    public class ProofCollection
    {
        public long Total { get; set; }
        public int? Skip { get; set; }
        public int? Limit { get; set; }
        public IEnumerable<Proof> Proofs { get; set; }
    }
}
