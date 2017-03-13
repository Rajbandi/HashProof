using System.Collections.Generic;
using HashProof.Core.Data;
using HashProof.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HashProof.Core.Services
{
    public class DataService : IDataService
    {
        private Serilog.ILogger _logger;
        private IDataRepository _repository;
        public DataService()
        {

        }

        protected Serilog.ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    var provider = DependencyResolver.Provider;
                    _logger = provider.GetService<Serilog.ILogger>();
                }
                return _logger;
            }
        }
        protected IDataRepository Repository
        {
            get
            {
                if (_repository == null)
                {
                    Logger.Information("Initialising Repository");
                    var provider = DependencyResolver.Provider;
                    _repository = provider.GetService<IDataRepository>();
                    
                }
                return _repository;
            }
        }

        #region Blocks related
        public BlockData GetLatestBlock()
        {
            Logger.Information("Retrieving get latest blocks");
            return Repository.GetLatestBlock();
        }

        public void SaveBlocks(IEnumerable<BlockData> blocks)
        {
            Logger.Information("Saving blocks");
            Repository.SaveBlocks(blocks);
        }

        public IEnumerable<BlockData> GetBlocks(BlockFilter filter)
        {
            Logger.Information("Get blocks ...");
            return Repository.GetBlocks(filter);
        }
      
        #endregion

        public ProofCollection GetProofs(ProofFilter proofFilter)
        {
            return Repository.GetProofs(proofFilter);
        }

        public void SaveProofs(IEnumerable<Proof> proofs)
        {
            Repository.SaveProofs(proofs);
        }

        public IEnumerable<Setting> GetSettings(SettingFilter settingFilter = null)
        {
            return Repository.GetSettings(settingFilter);
        }

        public void SaveSettings(IEnumerable<Setting> settings)
        {
             Repository.SaveSettings(settings);
        }
    }
}
