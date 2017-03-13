using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using HashProof.Core.Converters;
using HashProof.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using nStratis;
using nStratis.BIP32;
using nStratis.Protocol;
using nStratis.Protocol.Payloads;


namespace HashProof.Core.Services
{
    public class ProofService : IProofService
    {
       
        private readonly ExtKey _privateKey;
        private readonly ExtPubKey _publicKey;
        private string _keyPath;

        public int Fee { get; set; }

        private  IDataService _dataService;

        private string _nodes;
        public ProofService(string privateKey, string nodeAddr)
        {
            _privateKey = ExtKey.Parse(privateKey, Network.Main);
            _publicKey = _privateKey.Neuter();
            _nodes = nodeAddr;
        }

        private Serilog.ILogger _logger;

        private Serilog.ILogger Logger => _logger ?? (_logger = DependencyResolver.Provider.GetService<Serilog.ILogger>()); 
        private IDataService DataService => _dataService ?? (_dataService = DependencyResolver.Provider.GetService<IDataService>());

        public bool CheckProofPayment(string hash, string address)
        {
            var proofFilter = new ProofFilter
            {
                Hash = hash,
                Statuses = new[] { Constants.ProofStatus.ProofPending }
            };
            var proofs = DataService.GetProofs(proofFilter);
            return proofs.Proofs.Any();
        }

        public Proof GetProof(string hash)
        {
            var proofFilter = new ProofFilter
            {
                Hash = hash,
                Statuses = new[] { Constants.ProofStatus.Confirmed }
            };
            var proofs = DataService.GetProofs(proofFilter);
            return proofs.Proofs.FirstOrDefault();
        }

        public BlockData GetBlock(string blockId)
        {
            var blockFilter = new BlockFilter {BlockId = blockId};

            var blocks = DataService.GetBlocks(blockFilter);
            return blocks.FirstOrDefault();
        }

        public Proof CheckFee(string hash)
        {
            var proofFilter = new ProofFilter
            {
                Hash = hash,
                Statuses = new[] { Constants.ProofStatus.ProofPending, Constants.ProofStatus.ConfirmPending, Constants.ProofStatus.Confirmed}
            };
            var proofs = DataService.GetProofs(proofFilter);
            return proofs.Proofs.FirstOrDefault();
        }

        public bool CheckProof(string hash)
        {
            var proofFilter = new ProofFilter
            {
                Hash = hash,
                Statuses = new[] {Constants.ProofStatus.Confirmed}
            };
            var proofs = DataService.GetProofs(proofFilter);
            return proofs.Proofs.Any();
        }

        public Proof GenerateProof(string hash)
        {
            var settings = DataService.GetSettings().ToList();
            var lastKeyPath = settings.FirstOrDefault(x => x.Key == "KeyPath");
            var feeSetting = settings.FirstOrDefault(x => x.Key == "Fee");
            KeyPath keyPath;

            if (lastKeyPath != null)
            {
                keyPath = new KeyPath(lastKeyPath.Value);
                keyPath = keyPath.Increment();
            }
            else
            {
                keyPath = new KeyPath("0/0");
            }

            var newSettings = new List<Setting>
            {
                new Setting
                {
                    Key = Constants.Settings.KeyPath,
                    Value = keyPath.ToString()
                }
            };

            int fee;
            if (feeSetting == null)
            {
                fee = Fee;
                newSettings.Add(new Setting
                {
                    Key = Constants.Settings.Fee,
                    Value = Convert.ToString(fee)
                });
            }
            else
            {
                int.TryParse(feeSetting.Value, out fee);
                Fee = fee;
            }
            
            var pubKey = _publicKey.Derive(keyPath).PubKey;
            var pubKeyStr = pubKey.ToHex();
            var address = pubKey.GetAddress(Network.Main).ToString();
            var proof = new Proof
            {
                PayAddress = new WalletAddress(address, pubKeyStr, keyPath.ToString()),
                PayAmount = fee,
                Hash = hash,
                Status = Constants.ProofStatus.PaymentPending,
                DateCreated = DateTime.Now
            };

            DataService.SaveSettings(newSettings);
            DataService.SaveProofs(new [] {proof});

            return proof;
        }

        public ProofCollection GetPendingProofs(int? skip, int? limit, string search)
        {
            var proofFilter = new ProofFilter
            {
                Statuses = new [] { Constants.ProofStatus.PaymentPending, Constants.ProofStatus.ProofPending, Constants.ProofStatus.ConfirmPending},
                SortByDate = SortOrder.Descending,
                Skip = skip,
                Limit = limit,
                Search = search
            };
            return DataService.GetProofs(proofFilter);
        }

        public ProofCollection GetConfirmedProofs(int? skip, int? limit, string search)
        {
            var proofFilter = new ProofFilter { Statuses = new[]
            {
                Constants.ProofStatus.Confirmed ,
                
            },
                Skip = skip,
                Limit = limit,
            SortByDate = SortOrder.Descending,
                Search = search
            };
            return DataService.GetProofs(proofFilter);
        }

        public void CheckPendingPayments()
        {
            Logger.Information("Checking pending payments");
            var proofs = DataService.GetProofs(new ProofFilter
            {
                Statuses = new[] { Constants.ProofStatus.PaymentPending }
            }).Proofs;

            if (proofs != null)
            {
                Logger.Information($"Total pending proof payments {proofs.Count()}");
                var savedProofs = new List<Proof>();
                foreach (var proof in proofs)
                {
                    Logger.Information($"Checking balance { proof.PayAddress }");
                    var address = new BitcoinPubKeyAddress(proof.PayAddress.Address, Network.Main);
                    var scriptPubKey = address.ScriptPubKey.ToString();
                    var unspents = GetUnspentTransactions(address);

                    var balance = unspents.SelectMany(x => x.Outputs).
                        Where(x => x.ScriptPubKey == scriptPubKey).Sum(x => x.Value);
                    Logger.Information($"Retrieving balance { balance }");
                    if (balance >= proof.PayAmount)
                    {
                        Logger.Information($"Found balance {balance} >= {proof.PayAmount}");
                        proof.Status = Constants.ProofStatus.ProofPending;
                        proof.PayConfirmed = true;
                        proof.PayDate = DateTime.Now;
                        savedProofs.Add(proof);
                    }
                }

                if (savedProofs.Any())
                {
                    Logger.Information("Saving proofs");
                    DataService.SaveProofs(savedProofs);
                }

            }
            else
            {
                Logger.Information("No pending proof payments found");
            }
            Logger.Information("Check pending payments finished ");
        }

        public ExtKey GetSecret(string keyPath)
        {
            var key = _privateKey.Derive(new KeyPath(keyPath));

            return key;
        }

        public BlockData GetLatestBlock()
        {
            return DataService.GetLatestBlock();
        }

        public void SaveBlocks(IEnumerable<BlockData> blocks)
        {
            Logger.Information("Saving blocks in proof service");
            DataService.SaveBlocks(blocks);
        }

        public void CheckPendingProofs()
        {
            Logger.Information("Connecting to node ...");
            var node = Node.Connect(Network.Main, new IPEndPoint(IPAddress.Parse(_nodes), 16178));
            node.VersionHandshake();
            Logger.Information("Connected..");

            Logger.Information("Checking pending proofs");
            var proofs = DataService.GetProofs(new ProofFilter
            {
                Statuses = new[] { Constants.ProofStatus.ProofPending }
            }).Proofs;

            if (proofs != null)
            {
                Logger.Information($"Total pending proof payments {proofs.Count()}");
                var savedProofs = new List<Proof>();
                foreach (var proof in proofs)
                {
                    var privateKey = GetSecret(proof.PayAddress.KeyPath);
                    var secret = new BitcoinSecret(privateKey.PrivateKey, Network.Main);

                    var sender = _publicKey.Derive(new KeyPath("0/0"));
                    var senderAddress = sender.PubKey.GetAddress(Network.Main);
                    Logger.Information($"Checking balance { proof.PayAddress.Address }");
                    var address = new BitcoinPubKeyAddress(proof.PayAddress.Address, Network.Main);
                    var scriptPubKey = address.ScriptPubKey.ToString();
                    var unspents = GetUnspentTransactions(address).ToList();
                    //foreach (var unspent in unspents)
                    //{
                    //    Logger.Information($"-> {unspent.Hash}");
                    //}
                    var unspentTransaction = unspents.LastOrDefault(x=>x.IndexedOutputs.Any(o=>o.ScriptPubKey == scriptPubKey));
                    
                    if (unspentTransaction != null)
                    {
                        Logger.Information($"Unspent {unspentTransaction.Hash}");
                        var outputs = unspentTransaction.IndexedOutputs.Where(x => x.ScriptPubKey == scriptPubKey).ToList();
                        Logger.Information($"Total outputs found {outputs.Count}");
                        var transaction = new Transaction();
                        var balance = 0m;
                        var fee = 0.0001m;
                        var docFee = 0.00001m;

                        foreach (var output in outputs)
                        {
                            if (output.Value > 0)
                            {
                                balance += output.Value;
                                var txin = new TxIn
                                {
                                    PrevOut = new OutPoint(new uint256(unspentTransaction.Hash), output.Index),
                                    ScriptSig = secret.ScriptPubKey
                                };
                                transaction.Inputs.Add(txin);
                            }
                        }

                        Logger.Information($"Total inputs { transaction.Inputs.Count()} from prev ouputs {outputs.Count()} with balance {balance}");

                        if (balance > 0)
                        {
                            var amount = balance - (docFee + fee);


                            var docScriptKey = GetHashScript(proof.Hash);
                            Logger.Information($"docScriptKey -> {docScriptKey},  Length -> {docScriptKey.ToBytes().Length}");

                            var docOutput = new TxOut();
                            docOutput.Value = new Money(docFee, MoneyUnit.BTC);
                            docOutput.ScriptPubKey = docScriptKey;
                            transaction.AddOutput(docOutput);


                            var changeOutput = new TxOut();
                            changeOutput.Value = new Money(amount, MoneyUnit.BTC);
                            changeOutput.ScriptPubKey = senderAddress.ScriptPubKey;
                            transaction.AddOutput(changeOutput);

                            var isStandard = StandardScripts.IsStandardTransaction(transaction);
                            Logger.Information($"Standard -> {isStandard}");

                            transaction.Sign(secret, false);

                            Logger.Information($"{transaction}");


                        
                            node.SendMessage(new InvPayload(transaction));
                            node.SendMessage(new TxPayload(transaction));


                            proof.Status = Constants.ProofStatus.ConfirmPending;
                            savedProofs.Add(proof);
                        }
                    }
                }

                if (savedProofs.Any())
                {
                    Logger.Information("Saving pending proofs");
                    DataService.SaveProofs(savedProofs);
                }

            }
            else
            {
                Logger.Information("No pending proof payments found");
            }
            Logger.Information("Check pending proof finished ");

            Thread.Sleep(4000);
            node.Disconnect();
            Logger.Information("Node disconnecting...");
        }

        public Script GetHashScript(string hash)
        {
            var docHash = hash;

            var bytes = Encoding.UTF8.GetBytes(docHash);
            Console.WriteLine($"bytes : {bytes.Count()}");

            var newBytes = bytes;
            if (newBytes.Length > Constants.MaxOpReturn)
            {
                newBytes = newBytes.Take(Constants.MaxOpReturn).ToArray();
            }

            var docScriptKey = GenerateScriptPubKey(newBytes);
            return docScriptKey;
        }

        public void CheckConfirmedProofs()
        {
            Logger.Information("Checking confirm pending proofs ");
            var proofs = DataService.GetProofs(new ProofFilter
            {
                Statuses = new[] { Constants.ProofStatus.ConfirmPending }
            }).Proofs;

            if (proofs != null)
            {
                Logger.Information($"Total confirm pending proofs {proofs.Count()}");
                var savedProofs = new List<Proof>();
                foreach (var proof in proofs)
                {
                    var scriptPubKey = GetHashScript(proof.Hash);
                    var blocks = DataService.GetBlocks(new BlockFilter
                    {
                        ScriptPubKey = scriptPubKey.ToString()
                    }).ToList();

                    if (blocks.Any())
                    {
                        var block = blocks.First();

                        var transaction =
                            block.Transactions.FirstOrDefault(
                                x => x.Outputs.Any(o => o.ScriptPubKey == scriptPubKey.ToString()));

                        if (transaction != null)
                        {
                            Logger.Information($"Proof confirmed on Block {block.Height} TxId {transaction.Hash}");
                            proof.BlockId = block.Hash;
                            proof.BlockHeight = block.Height;
                            proof.TxId = transaction.Hash;
                            proof.Status = Constants.ProofStatus.Confirmed;
                            savedProofs.Add(proof);
                        }
                    }
                }

                if (savedProofs.Any())
                {
                    Logger.Information("Saving proofs");
                    DataService.SaveProofs(savedProofs);
                }

            }
            else
            {
                Logger.Information("No pending confirm proofs found");
            }
            Logger.Information("Check confirm pending proofs finished ");
        }

        public Script GenerateScriptPubKey(params byte[][] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var ops = new Op[data.Length + 1];
            ops[0] = OpcodeType.OP_RETURN;
            for (var i = 0; i < data.Length; i++)
            {
                ops[1 + i] = Op.GetPushOp(data[i]);
            }
            var script = new Script(ops);
            var MaxScriptSizeLimit = 40;
            Logger.Information($"bytes : {script.ToBytes(true).Length}");
            if (script.ToBytes(true).Length > MaxScriptSizeLimit)
                throw new ArgumentOutOfRangeException("data", "Data in OP_RETURN should have a maximum size of " + MaxScriptSizeLimit + " bytes");
            return script;
        }

        public IEnumerable<TransactionData> GetUnspentTransactions(BitcoinPubKeyAddress address)
        {

            var scriptPubKey = address.ScriptPubKey.ToString();

            var blocks = DataService.GetBlocks(new BlockFilter
            {
                ScriptPubKey = scriptPubKey
            });

            Logger.Information($"Total unspent transactions {blocks.Count()}");

            return blocks.SelectMany(x => x.Transactions);
        }
    }
}
