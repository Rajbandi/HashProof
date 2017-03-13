using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using HashProof.Core.Data;
using HashProof.Core.Models;
using nStratis;

namespace HashProof.Core.Services
{
    public interface IDataService
    {
        void SaveBlocks(IEnumerable<BlockData> blocks);
        BlockData GetLatestBlock();
        IEnumerable<BlockData> GetBlocks(BlockFilter filter);

        ProofCollection GetProofs(ProofFilter proofFilter);
        void SaveProofs(IEnumerable<Proof> proofs);

        IEnumerable<Setting> GetSettings(SettingFilter settingFilter=null);
        void SaveSettings(IEnumerable<Setting> settings);

    }
}
