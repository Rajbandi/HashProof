using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HashProof.Core.Models;
using nStratis;
using nStratis.BIP32;

namespace HashProof.Core.Services
{
    public interface IProofService
    {
        Proof GenerateProof(string hash);
        ProofCollection GetPendingProofs(int? skip, int? limit,string search);
        ProofCollection GetConfirmedProofs(int? skip, int? limit, string search);
        bool CheckProof(string hash);
        BlockData GetBlock(string blockId);
        Proof CheckFee(string hash);
        void CheckPendingPayments();
        ExtKey GetSecret(string keyPath);
        void CheckPendingProofs();
        Script GenerateScriptPubKey(params byte[][] data);
        IEnumerable<TransactionData> GetUnspentTransactions(BitcoinPubKeyAddress address);
        BlockData GetLatestBlock();
        void SaveBlocks(IEnumerable<BlockData> blocks);
        Script GetHashScript(string hash);
        void CheckConfirmedProofs();
    }
}
