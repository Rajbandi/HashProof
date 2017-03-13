using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HashProof.Core.Models;
using nStratis;
using Wallet = nStratis.SPV.Wallet;

namespace HashProof.Core.Data
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDataRepository
    {
        ProofCollection GetProofs(ProofFilter proofFilter = null);
        void SaveProofs(IEnumerable<Proof> proofs);

        IEnumerable<Setting> GetSettings(SettingFilter settingFilter = null);
        void SaveSettings(IEnumerable<Setting> settings);
        
        IEnumerable<BlockData> GetBlocks(BlockFilter blockFilter = null);
        int SaveBlocks(IEnumerable<BlockData> blocks);

        BlockData GetLatestBlock();
     

    }
}
