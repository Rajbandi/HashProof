using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HashProof.Core
{
    public static class Constants
    {
        public static int MaxOpReturn = 38;
        public static class Settings
        {
            public const string Fee = "Fee";
            public const string KeyPath = "KeyPath";
        }
        public static class ProofStatus
        {
            public const string  PaymentPending = "PaymentPending";
            public const string ProofPending = "ProofPending";
            public const string ConfirmPending = "ConfirmPending";
            public const string Confirmed = "Confirmed";
        }

    }
}
