using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using HashProof.Core;
using HashProof.Core.Helpers;
using HashProof.Core.Models;
using HashProof.Core.Services;
using nStratis;
using nStratis.Protocol;
using nStratis.BitcoinCore;
using nStratis.BIP32;
using nStratis.DataEncoders;
using nStratis.Protocol.Behaviors;
using nStratis.Protocol.Payloads;
using Microsoft.Extensions.DependencyInjection;

namespace HashProof.Sync
{
    public class SyncSettings
    {
        public Serilog.ILogger Logger { get; set; }
        public WalletSettings Wallet { get; set; }
        public Network Network { get; set; }
        public string AppDir { get; set; }
        public IServiceProvider Provider { get; set; }
        public string Nodes { get; set; }
    }

    public class WalletSettings
    {
        public string MasterPrivKey { get; set; }
        public KeyPath KeyPath { get; set; }
        public Serilog.ILogger Logger { get; set; }
    }

    public class SyncAddress
    {
        public SyncAddress(PubKey pubKey, KeyPath keypath, Network network)
        {

            Address = pubKey.GetAddress(network);
            KeyPath = keypath;
            PubKey = pubKey;
        }

        public int ScanHeight { get; set; }
        public PubKey PubKey { get; }
        public BitcoinPubKeyAddress Address { get; }

        public KeyPath KeyPath { get; }

        public decimal Balance { get; set; }

        public bool IsScanned { get; set; }

        public readonly List<Transaction> SpentTransactions = new List<Transaction>();
        public readonly List<Transaction> UnspentTransactions = new List<Transaction>();
        public int LastTxCount { get; set; }

    }
    public class SyncWallet
    {
        private readonly ExtKey _masterPrivKey;
        private readonly ExtPubKey _masterPubKey;
        private readonly KeyPath _keyPath;
        private KeyPath _curPath;

        private readonly IList<SyncAddress> _addresses = new List<SyncAddress>();
        public SyncWallet(WalletSettings settings, Network network)
        {
            _masterPrivKey = ExtKey.Parse(settings.MasterPrivKey);
            _masterPubKey = _masterPrivKey.Neuter();
            _keyPath = settings.KeyPath ?? new KeyPath("0/0");
            _curPath = new KeyPath(_keyPath.Indexes);
            for (var index = 0; index < 1; index++)
            {
                var pubkey = _masterPubKey.Derive(_curPath).PubKey;
                var address = new SyncAddress(pubkey, _curPath, network);
                _addresses.Add(address);
                _curPath = _curPath.Increment();
            }
            //_keyPath.Increment();
        }

        public IList<SyncAddress> Addresses => _addresses;
    }
    public class NodeSync
    {
        private const int ChunkSize = 1000;
        private readonly SyncSettings _settings;
        private readonly Serilog.ILogger _logger;
        private readonly SyncWallet _wallet;
        private IProofService _proofService;
        public NodeSync(SyncSettings settings)
        {
            _settings = settings;
            _proofService = _settings.Provider.GetService<IProofService>();

            var network = settings.Network ?? Network.Main;

            _logger = settings.Logger;
            _appDir = settings.AppDir;
            _wallet = new SyncWallet(settings.Wallet, network);
            //var parameters = new NodeConnectionParameters();
            //parameters.TemplateBehaviors.Add(new AddressManagerBehavior(GetAddressManager()));
            //_group = new NodesGroup(_settings.Network, parameters, new NodeRequirement()
            //{
            //    RequiredServices = NodeServices.Network
            //});
            //_group.MaximumNodeConnection = 4;
            //_logger.Information("Connecting to nodes ");
            //_group.Connect();

            _logger.Information("Initiated node sync");

        }

        private const string ChainFileName = "chain.dat";
        private readonly string _appDir;
        private ConcurrentChain _chain;
        private NodesGroup _group;
        private BlockStore _blockStore;
        private int _lastHeight;
        private bool _isRunning;

        private AddressManager GetAddressManager()
        {
            try
            {
                return AddressManager.LoadPeerFile(AddrmanFile());
            }
            catch
            {
                return new AddressManager();
            }
        }
        private string AddrmanFile()
        {
            return Path.Combine(_appDir, "addrman.dat");
        }

        private string ChainFile() => Path.Combine(_appDir, ChainFileName);

        private void LoadChain()
        {
            _logger.Information("Checking chain file on disk ... ");
            if (File.Exists(ChainFile()))
            {
                _logger.Information("Exists, loading now..");
                _chain = new ConcurrentChain(_settings.Network);
                _chain.Load(File.ReadAllBytes(ChainFile()));
            }
        }
        private void SaveChain()
        {
            if (!Directory.Exists(_appDir))
            {
                Directory.CreateDirectory(_appDir);
            }
            using (var fs = File.Open(ChainFile(), FileMode.Create))
            {
                _chain.WriteTo(fs);
            }
        }

        public void Sync()
        {
            _isRunning = true;
            try
            {
                var node = Node.Connect(Network.Main, new IPEndPoint(IPAddress.Parse(_settings.Nodes), 16178));
                // var node = Node.ConnectToLocal(Network.Main);

                //var connectedNodes = _group.ConnectedNodes.ToArray();
                //if (!connectedNodes.Any())
                //{
                //    _logger.Information("Waiting for nodes ... ");
                //    _isRunning = false;
                //    return;
                //}
                _logger.Information("************************************************");
                _logger.Information("Connecting to Node ");
                //_logger.Information($"Total connected nodes {connectedNodes.Length}");
                //var rand = new Random();
                //var node = connectedNodes[rand.Next(connectedNodes.Length)];
                _logger.Information($"Node selected {node.Peer.Endpoint.Address}");
                if (node.IsConnected)
                {
                    node.VersionHandshake();

                    _logger.Information("Connected...");
                    _logger.Information($"Last Seen -->{node.LastSeen}");
                    _logger.Information("Checking chain");
                    if (_chain == null)
                    {
                        _logger.Information("Loading chain from disk...");
                        LoadChain();
                        if (_chain == null)
                        {
                            _logger.Information("No chain found on disk, Syncing chain from node ");
                            _chain = node.GetChain();
                            _logger.Information("Chain sync completed");
                        }
                    }

                    _logger.Information("Retrieving latest block...");
                    var lastDbBlock = _proofService.GetLatestBlock();
                    _logger.Information($"Latest Block {lastDbBlock}");

                    _logger.Information("Synching latest chain...");
                    node.SynchronizeChain(_chain);
                    _lastHeight = _chain.Height;

                    _logger.Information($"Lastest Chain Height: {_lastHeight}");

                    var height = 0;
                    IEnumerable<IEnumerable<uint256>> hashChunks = null;
                    if (lastDbBlock == null)
                    {
                        _logger.Information("Retrieving chained blocks from height 0");
                        hashChunks = _chain.ToEnumerable(false).Select(x => x.HashBlock).Chunk(ChunkSize);
                    }
                    else
                    if (lastDbBlock.Height < _chain.Height)
                    {
                        _logger.Information($"Retrieving chained blocks from height {lastDbBlock.Height}");
                        hashChunks = _chain.EnumerateAfter(new uint256(lastDbBlock.Hash)).Select(x => x.HashBlock).Chunk(ChunkSize);
                        height = lastDbBlock.Height + 1;
                    }
                    if (hashChunks != null)
                    {
                        _logger.Information($"Total blocks to save {_chain.Height - (lastDbBlock ?? new BlockData()).Height}, hashChunks {hashChunks.LongCount()}");
                        var chunkSize = 0;
                        foreach (var hashChunk in hashChunks)
                        {
                            _logger.Information($"Retrieving chunks from {chunkSize} - {chunkSize + ChunkSize}");
                            var blocks = node.GetBlocks(hashChunk);
                            var dataBlocks = blocks.Select(x =>
                            {
                                var block = BlockData.Parse(x);
                                block.Height = height++;
                                return block;
                            });
                            _proofService.SaveBlocks(dataBlocks);
                            chunkSize += ChunkSize;
                            _logger.Information($"Saved blocks");
                        }
                    }
                    _logger.Information("Finished syncing...");
                    SaveChain();

                    _logger.Information("Disconnecting node....");
                    node.Disconnect();

                    CheckPending();

                    //var indexStore = new IndexedBlockStore(new InMemoryNoSqlRepository(), _blockStore);

                    // CheckAddressTransactions(newBlocks);

                    _logger.Information("************************************************");
                }

                else
                {
                    _logger.Information("Couldn't connect to node");
                }
            }
            catch (System.Exception ex)
            {
                _logger.Information($"Exception while syncing {ex}");
            }
            finally
            {
                _isRunning = false;
            }
        }

        public void CheckPending()
        {
            _logger.Information("Checking pending proofs");
            _proofService.CheckPendingPayments();

            _proofService.CheckPendingProofs();

            _proofService.CheckConfirmedProofs();
        }



        public bool IsRunning => _isRunning;

    }

}

