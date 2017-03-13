using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using HashProof.Core.Helpers;
using HashProof.Core.Models;
using HashProof.Core.Models.Mongo;
using HashProof.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using nStratis;
using nStratis.BIP32;


namespace HashProof.Core.Data
{
    public class MongoDbRepository : IDataRepository
    {
        private IMongoClient _dbClient;

        private const string ChainedBlocks = "ChainedBlocks";
        private const string Blocks = "Blocks";
        private const string Proofs = "Proofs";
        private const string Settings = "Settings";
        private const string Addresses = "Addresses";

        private readonly string _connString;
        private readonly IMapper _mapper;
        public MongoDbRepository(string connString)
        {
            _connString = connString;
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<BlockData, MongoBlockData>();
                cfg.CreateMap<ChainedBlockData, MongoChainedBlockData>();
                cfg.CreateMap<Proof, MongoProof>();
                cfg.CreateMap<Setting, MongoSetting>();

            });
            _mapper = config.CreateMapper();
        }

        private IMongoDatabase _database;
        public IMongoDatabase Database
        {
            get
            {
                if (_database == null)
                {

                    _dbClient = new MongoClient(new MongoUrl(_connString));
                    _database = _dbClient.GetDatabase("HashProof");
                }
                return _database;
            }
        }

        public ProofCollection GetProofs(ProofFilter proofFilter = null)
        {
            List<MongoProof> data;
            var proofCollection = new ProofCollection();
            
            var collection = Database.GetCollection<MongoProof>(Proofs);
            var builder = Builders<MongoProof>.Filter;
            var filterDefs = new List<FilterDefinition<MongoProof>>();
            if (proofFilter != null)
            {
                if (!string.IsNullOrWhiteSpace(proofFilter.Hash))
                {
                    filterDefs.Add(builder.Eq(x => x.Hash, proofFilter.Hash));
                }

                if (!string.IsNullOrWhiteSpace(proofFilter.Address))
                {
                    filterDefs.Add(builder.Eq(x => x.PayAddress.Address, proofFilter.Address));
                }

                if (proofFilter.Statuses != null && proofFilter.Statuses.Any())
                {
                    filterDefs.Add(builder.In(x => x.Status, proofFilter.Statuses));
                }

                if (proofFilter.PayAmount.HasValue)
                {
                    filterDefs.Add(builder.Eq(x => x.PayAmount, proofFilter.PayAmount.Value));
                }
                if (proofFilter.PayConfirmed.HasValue)
                {
                    filterDefs.Add(builder.Eq(x => x.PayConfirmed, proofFilter.PayConfirmed.Value));
                }
                if (proofFilter.PayDate.HasValue)
                {
                    filterDefs.Add(builder.Eq(x => x.PayDate, proofFilter.PayDate));
                }
                if (!string.IsNullOrWhiteSpace(proofFilter.Search))
                {
                    filterDefs.Add(builder.Or(new[]
                    {
                        builder.Regex(x => x.PayAddress.Address, new BsonRegularExpression(proofFilter.Search)),
                        builder.Regex(x => x.Hash, new BsonRegularExpression(proofFilter.Search)),
                    }));
                }

                var filterDef = filterDefs.Any() ? filterDefs.Aggregate((x, y) => x & y) : builder.Empty;
                 var recs = collection.Find<MongoProof>(filterDef);

                proofCollection.Total = recs.Count(); 

                if (proofFilter.SortByDate == SortOrder.Ascending)
                {
                    recs = recs.SortBy(x => x.DateCreated);
                }
                else
                if(proofFilter.SortByDate == SortOrder.Descending)
                {
                    recs = recs.SortByDescending(x => x.DateCreated);
                }
                if (proofFilter.Skip.HasValue)
                {
                    recs = recs.Skip(proofFilter.Skip);
                    proofCollection.Skip = proofFilter.Skip;
                }
                if (proofFilter.Limit.HasValue)
                {
                    recs = recs.Limit(proofFilter.Limit);
                    proofCollection.Limit = proofFilter.Limit;
                }
                data = recs.ToList();
                proofCollection.Proofs = data;
            }
            else
            {
                
                data = collection.Find<MongoProof>(builder.Empty).ToList();
                proofCollection.Total = data.Count();
                proofCollection.Proofs = data;
            }

            return proofCollection;
        }

        public void SaveProofs(IEnumerable<Proof> proofs)
        {
            var collection = Database.GetCollection<MongoProof>(Proofs);
            var models = new List<WriteModel<MongoProof>>();
            var builder = Builders<MongoProof>.Filter;
            foreach (var proof in proofs)
            {
                var data = _mapper.Map<Proof, MongoProof>(proof);
                var filterDef = builder.Eq(x => x.Hash, data.Hash);
                models.Add(new ReplaceOneModel<MongoProof>(filterDef, data) { IsUpsert = true });
            };
            collection.BulkWriteAsync(models.AsEnumerable());
        }

        public IEnumerable<Setting> GetSettings(SettingFilter settingFiler = null)
        {
            var collection = Database.GetCollection<MongoSetting>(Settings);
            var builder = Builders<MongoSetting>.Filter;
            var filterDefs = new List<FilterDefinition<MongoSetting>>();
            if (settingFiler != null)
            {
                if (!string.IsNullOrWhiteSpace(settingFiler.Key))
                {
                    filterDefs.Add(builder.Eq(x => x.Key, settingFiler.Key));
                }

                if (!string.IsNullOrWhiteSpace(settingFiler.Value))
                {
                    filterDefs.Add(builder.Eq(x => x.Value, settingFiler.Value));
                }
            }

            var filterDef = filterDefs.Any() ? filterDefs.Aggregate((x, y) => x & y) : builder.Empty;
            var data = collection.Find<MongoSetting>(filterDef).ToList();
            return data;
        }

        public void SaveSettings(IEnumerable<Setting> settings)
        {
            var collection = Database.GetCollection<MongoSetting>(Settings);
            var models = new List<WriteModel<MongoSetting>>();
            var builder = Builders<MongoSetting>.Filter;
            foreach (var setting in settings)
            {
                var data = _mapper.Map<Setting, MongoSetting>(setting);
                var filterDef = builder.Eq(x => x.Key, data.Key);
                models.Add(new ReplaceOneModel<MongoSetting>(filterDef, data) { IsUpsert = true });
            };
            collection.BulkWriteAsync(models.AsEnumerable());
        }


        #region Blocks  

        public IEnumerable<BlockData> GetBlocks(BlockFilter blockFilter = null)
        {
            var blockData = new List<BlockData>();
            var collection = Database.GetCollection<MongoBlockData>(Blocks);
            var builder = Builders<MongoBlockData>.Filter;
            var filterDefs = new List<FilterDefinition<MongoBlockData>>();
            if (blockFilter != null)
            {
                if (!string.IsNullOrWhiteSpace(blockFilter.BlockId))
                {
                    filterDefs.Add(builder.Eq(x => x.Hash, blockFilter.BlockId));
                }

                if (!string.IsNullOrWhiteSpace(blockFilter.TxId))
                {
                    filterDefs.Add(builder.Where(x => x.Transactions.Select(t => t.Hash).Contains(blockFilter.TxId)));
                }

                if (!string.IsNullOrWhiteSpace(blockFilter.ScriptPubKey))
                {
                    filterDefs.Add(builder.Where(x => x.Transactions.Any(t => t.Outputs.Any(o => o.ScriptPubKey == blockFilter.ScriptPubKey))));
                }

                if (blockFilter.UnSpents != null && blockFilter.UnSpents.Any())
                {
                    filterDefs.Add(builder.Where(x => x.Transactions.Any(t => t.Inputs.Any(i => blockFilter.UnSpents.Contains(i.PrevOut.Hash)))));
                }
                var filterDef = filterDefs.Any() ? filterDefs.Aggregate((x, y) => x & y) : builder.Empty;
                var blocks = collection.Find<MongoBlockData>(filterDef).ToList();
                if (!string.IsNullOrWhiteSpace(blockFilter.SenderPubKey))
                {
                    var blocksSpents =
                        blocks.Where(
                            b =>
                                b.Transactions.Any(
                                    t =>
                                        t.Inputs.Any(
                                            i => TransactionHelper.IsFrom(blockFilter.SenderPubKey, i.ScriptSig))));
                    foreach (var block in blocksSpents)
                    {
                        blockData.Add(block);
                    }
                }
                else
                {
                    foreach (var block in blocks)
                    {
                        blockData.Add(block);
                    }
                }

            }

           
            return blockData;
        }

        public int SaveBlocks(IEnumerable<BlockData> blocks)
        {
            var collection = Database.GetCollection<MongoBlockData>(Blocks);
            var models = new List<WriteModel<MongoBlockData>>();
            var builder = Builders<MongoBlockData>.Filter;
            foreach (var block in blocks)
            {
                var dataBlock = _mapper.Map<BlockData, MongoBlockData>(block);
                var filterDef = builder.Eq(x => x.Hash, dataBlock.Hash);
                models.Add(new ReplaceOneModel<MongoBlockData>(filterDef, dataBlock) { IsUpsert = true });
            };
            collection.BulkWriteAsync(models.AsEnumerable());
            return 0;
        }

        public BlockData GetLatestBlock()
        {
            var collection = Database.GetCollection<MongoBlockData>(Blocks);
            var filterBuilder = Builders<MongoBlockData>.Filter;
            var sortBuilder = Builders<MongoBlockData>.Sort;
            var filter = filterBuilder.Empty;
            var sort = sortBuilder.Descending(x => x.Header.Time);
            return collection.Find(filter).Sort(sort).FirstOrDefault();
        }

        #endregion

    }
}
