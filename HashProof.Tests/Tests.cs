using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HashProof.Core.Data;
using HashProof.Core.Helpers;
using HashProof.Core.Models;
using HashProof.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nStratis;
using nStratis.OpenAsset;
using Serilog;


namespace Tests
{
    [TestClass]
    public class Tests
    {
        private IServiceProvider _provider;

        private Serilog.ILogger _logger;

        [TestInitialize]
        public void Init()
        {
            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.Debug()
              .WriteTo.ColoredConsole()
              .CreateLogger();

            _logger = Log.Logger;

            var services = new ServiceCollection();
            var dbSettings = new MongoDbSettings();
            dbSettings.Server ="127.0.0.1";
            dbSettings.DbName = "HashProof";

            services.AddSingleton<IDataRepository>(new MongoDbRepository(dbSettings));
            services.AddSingleton<IDataService>(new DataService());
            services.AddSingleton<Serilog.ILogger>(Log.Logger);
            
            _provider = services.BuildServiceProvider();
            DependencyResolver.Provider = _provider;
        }

        [TestMethod]
        public void TestUnSpentTransactions()
        {
            var addr = "SfFsHYhj3ao6DP7KvbLuCtmGD45Y4gr6Wz";
            var transactions = GetUnspentTransactions(addr);
            Assert.IsTrue(transactions.Any());
        }

        [TestMethod]
        public void GetBalance()
        {
            var balance = 0.0m;
            var addr = "SfFsHYhj3ao6DP7KvbLuCtmGD45Y4gr6Wz";
            var address = new BitcoinPubKeyAddress(addr, Network.Main);
         
            var unspentTransactions = GetUnspentTransactions(addr);
            var unspents = unspentTransactions.Select(x => x.Hash).ToList();
            var pubKey = "02ebe13d606e16c671e108a6c3c61f51235240124bbe785adeca1d3b788c858098";
            var spentTransactions = GetSpentTransactions(unspents, pubKey);
            foreach (var unspentTx in unspentTransactions)
            {
                var val = unspentTx.Outputs
                    .Where(x => x.ScriptPubKey == address.ScriptPubKey.ToString())
                    .Sum(x => x.Value);

                balance = Math.Abs(balance + val);
                _logger.Information($"Unspent balance {val}");
                var spentTx = spentTransactions.FirstOrDefault(x => x.Inputs.Any(i => i.PrevOut.Hash == unspentTx.Hash));
                if (spentTx != null)
                {
                    //var coins = new TransactionBuilder().FindSpentCoins(spentTx);
                    //var fee = spentTx.GetFee(coins).ToDecimal(MoneyUnit.BTC);
                    //var feeRate = new FeeRate(new Money(2000));
                    //var fee = feeRate.GetFee(spentTx.GetSerializedSize()).ToDecimal(MoneyUnit.BTC);
                    var fee = 0.0m;

                    var spentMoney = spentTx.Outputs.Where(x => x.ScriptPubKey != address.ScriptPubKey.ToString())
                                            .Sum(x => x.Value);
                    var changeMoney = spentTx.Outputs.Where(x => x.ScriptPubKey == address.ScriptPubKey.ToString())
                                            .Sum(x => x.Value);
                    if (changeMoney == 0)
                    {
                        balance = Math.Abs(balance - Math.Abs(val - spentMoney));
                    }
                    else
                    {
                        balance = Math.Abs(balance - Math.Abs(val - changeMoney));
                    }
                    _logger.Information($"Spent money {spentMoney} Fee {fee}");
                    balance = Math.Abs(balance - (spentMoney + fee));
                    
                }
            }

            Debug.WriteLine($"Balance {balance}");
        }

        [TestMethod]
        public void TestSpentTransactions()
        {
            var addr = "SfFsHYhj3ao6DP7KvbLuCtmGD45Y4gr6Wz";
            var address = new BitcoinPubKeyAddress(addr, Network.Main);
            Assert.AreEqual(addr, address.Hash.GetAddress(Network.Main).ToString(), "The given stratis address is invalid");
            var transactions = GetUnspentTransactions(addr).ToList();
            var unspents = transactions.Select(t => t.Hash);
            var pubKey = "02ebe13d606e16c671e108a6c3c61f51235240124bbe785adeca1d3b788c858098";
            var spentTransactions = GetSpentTransactions(unspents, pubKey);
            Assert.IsTrue(spentTransactions.Any());
        }
        public IEnumerable<TransactionData> GetSpentTransactions(IEnumerable<string> unspents, string pubKey )
        {

            var dataService = _provider.GetService<IDataService>();
            var blocks = dataService.GetBlocks(new BlockFilter
            {
                UnSpents =  unspents,
                SenderPubKey = pubKey
            });

            return blocks.SelectMany(x => x.Transactions);
        }
        public IEnumerable<TransactionData> GetUnspentTransactions(string addr)
        {
            var address = new BitcoinPubKeyAddress(addr, Network.Main);
            Assert.AreEqual(addr, address.Hash.GetAddress(Network.Main).ToString(), "The given stratis address is invalid");
            var scriptPubKey = address.ScriptPubKey.ToString();
            var dataService = _provider.GetService<IDataService>();
            var blocks = dataService.GetBlocks(new BlockFilter
            {
                ScriptPubKey = scriptPubKey
            });

            return blocks.SelectMany(x => x.Transactions);
        }


        //private void CheckAddressTransactions(IList<Block> newBlocks)
        //{
        //    _logger.Information("Checking address balances ...");
        //    foreach (var address in _wallet.Addresses)
        //    {
        //        _logger.Information(
        //            $"Checking with address {address.Address}, scriptpubkey {address.Address.ScriptPubKey}, Scan Height {address.ScanHeight}, Balance {address.Balance}");


        //        var unspentTransactions = new List<Transaction>();
        //        var spentTransactions = new List<Transaction>();

        //        if (address.ScanHeight < _chain.Height)
        //        {
        //            _logger.Information("Scanning from blockchain");
        //            var transactions = _blockStore.Enumerate(false)
        //                            .Skip(address.ScanHeight)
        //                            .SelectMany(b => b.Item.Transactions);

        //            _logger.Information($"Total transactions {transactions.Count()} for scanning");

        //            unspentTransactions.AddRange(transactions
        //                .Where(t => t.Outputs.Any(o => o.ScriptPubKey == address.Address.ScriptPubKey))
        //                .ToList());
        //            _logger.Information($"Unspent transactions found {unspentTransactions.Count}");
        //            var txs = unspentTransactions.Select(x => x.GetHash()).ToList();
        //            var pubkey = address.PubKey;
        //            _logger.Information($"Address pubkey {pubkey}");

        //            var uhashes = address.UnspentTransactions.Select(x => x.GetHash()).ToList();

        //            foreach (var tx in unspentTransactions)
        //            {
        //                if (!uhashes.Contains(tx.GetHash()))
        //                {
        //                    address.UnspentTransactions.Add(tx);
        //                }
        //            }
        //            spentTransactions.AddRange(transactions
        //              .Where(t => t.Inputs.Any(i => txs.Contains(i.PrevOut.Hash)) && t.Inputs.Any(i => i.IsFrom(pubkey)))
        //              .ToList());
        //            _logger.Information($"spent transactions found {spentTransactions.Count}");

        //            var shashes = address.SpentTransactions.Select(x => x.GetHash()).ToList();
        //            foreach (var tx in spentTransactions)
        //            {
        //                if (!shashes.Contains(tx.GetHash()))
        //                {
        //                    address.SpentTransactions.Add(tx);
        //                }
        //            }
        //            address.ScanHeight = _chain.Height;
        //            CheckBalance(address);
        //        }

        //    }
        //}

        //private void CheckBalance(SyncAddress address)
        //{
        //    var count = address.UnspentTransactions.Count() + address.SpentTransactions.Count();
        //    _logger.Information($"Address Tx Count {count}, Last Count {address.LastTxCount}");
        //    if (count == address.LastTxCount)
        //    {
        //        _logger.Information($"Skipping balance check ");
        //        return;
        //    }

        //    _logger.Information($"Checking balance...");
        //    var balance = 0.0m;
        //    foreach (var unspentTx in address.UnspentTransactions)
        //    {
        //        var val = unspentTx.Outputs
        //            .Where(x => x.ScriptPubKey == address.Address.ScriptPubKey)
        //            .Sum(x => x.Value.ToDecimal(MoneyUnit.BTC));

        //        balance = Math.Abs(balance + val);
        //        _logger.Information($"Unspent balance {val}");
        //        var spentTx = address.SpentTransactions.FirstOrDefault(x => x.Inputs.Any(i => i.PrevOut.Hash == unspentTx.GetHash()));
        //        if (spentTx != null)
        //        {
        //            //var coins = new TransactionBuilder().FindSpentCoins(spentTx);
        //            //var fee = spentTx.GetFee(coins).ToDecimal(MoneyUnit.BTC);
        //            //var feeRate = new FeeRate(new Money(2000));
        //            //var fee = feeRate.GetFee(spentTx.GetSerializedSize()).ToDecimal(MoneyUnit.BTC);
        //            var fee = 0.0m;

        //            var spentMoney = spentTx.Outputs.Where(x => x.ScriptPubKey != address.Address.ScriptPubKey)
        //                                    .Sum(x => x.Value.ToDecimal(MoneyUnit.BTC));
        //            var changeMoney = spentTx.Outputs.Where(x => x.ScriptPubKey == address.Address.ScriptPubKey)
        //                                    .Sum(x => x.Value.ToDecimal(MoneyUnit.BTC));
        //            if (changeMoney == 0)
        //            {
        //                balance = Math.Abs(balance - Math.Abs(val - spentMoney));
        //            }
        //            else
        //            {
        //                balance = Math.Abs(balance - Math.Abs(val - changeMoney));
        //            }
        //            _logger.Information($"Spent money {spentMoney} Fee {fee}");
        //            balance = Math.Abs(balance - (spentMoney + fee));
        //        }
        //    }

        //    _logger.Information($"Total Balance {balance}");
        //    address.Balance = balance;
        //    address.LastTxCount = address.UnspentTransactions.Count() + address.SpentTransactions.Count();
        //}

        [TestCleanup]
        public void Dispose()
        {
            
        }
    }
}
