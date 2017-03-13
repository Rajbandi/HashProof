using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using nStratis;
using nStratis.BIP32;
using nStratis.Protocol;
using nStratis.Protocol.Behaviors;
using nStratis.Protocol.Payloads;
using nStratis.SPV;

namespace HashProof.SPV
{
    public class SPVSettings
    {
        public BitcoinExtPubKey RootKey { get; set; }
        public string AppDir { get; set; }
        public Network Network { get; set; }
        public Serilog.ILogger Logger { get; set; }
    }
    public class SPVWallet
    {
        private readonly SPVSettings _settings;
        private Wallet _wallet;
        private NodeConnectionParameters _connectionParameters;
        private bool _disposed;
        private NodesGroup _group;
        private readonly object _saving = new object();
        private readonly Serilog.ILogger _logger;
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        private bool _isNewWallet;
        public SPVWallet(SPVSettings settings)
        {
            _logger = settings.Logger;

            _logger.Information("SPV Wallet starting");
            _settings = settings;
            _logger.Information("SPV Wallet init");

            if (!File.Exists(WalletFile()))
            {
                _logger.Information("Wallet Doesn't exist, creating new one");

                var walletCreation = new WalletCreation
                {
                    Name = "DefaultWallet",
                    RootKeys = new[] { (ExtPubKey)_settings.RootKey },
                    UseP2SH = false,
                    Network = _settings.Network,
                    SignatureRequired = 1

                };
                _wallet = new Wallet(walletCreation, 1);
                _isNewWallet = true;
            }
            else
            {
                _logger.Information("Wallet already exist");
                _logger.Information("Loading SPV Wallet");
                LoadWallet();
            }

            _logger.Information("Start connecting");

            StartConnecting().Wait();
        }

        private async Task StartConnecting()
        {
            await Task.Factory.StartNew(() =>
            {

                var parameters = new NodeConnectionParameters();
                parameters.TemplateBehaviors.Add(new AddressManagerBehavior(GetAddressManager()));
                parameters.TemplateBehaviors.Add(new ChainBehavior(GetChain()));
                parameters.TemplateBehaviors.Add(new TrackerBehavior(GetTracker()));
                if (!_disposed)
                {
                    _group = new NodesGroup(_settings.Network, parameters, new NodeRequirement()
                    {
                        RequiredServices = NodeServices.Network
                    });
                    _group.MaximumNodeConnection = 4;
                    _group.Connect();
                    _connectionParameters = parameters;
                }

            });

            //   PeriodicSave();
           

            _logger.Information("Configuring groups");
            _wallet.Configure(_group);
            if (_isNewWallet)
            {
                _logger.Information("New wallet, generating key pool ");
                //_wallet.GetNextScriptPubKey();
                GenerateNewAddress();
                SaveWallet();
            }
            _logger.Information("Connecting Wallet");
            PeriodicKick();
            _wallet.NewWalletTransaction += _wallet_NewWalletTransaction;
            
            _wallet.Connect();
            _logger.Information("Wallet connected");
            _lastHeight = CurrentHeight;
            //while (!_disposed)
            //{
            //    await Task.Delay(10000);
            //    _logger.Information($"Current Height {CurrentHeight}");
            //    _logger.Information($"Connected Nodes {ConnectedNodes.Count}");
            //    SaveAsync();
            //}
        }

        public void GenerateNewAddress()
        {
            var script = _wallet.GetNextScriptPubKey();
            _logger.Information($"Address {script.GetDestinationAddress(_settings.Network)}");
            
        }
        private int _lastHeight;
        public void Run()
        {
            _logger.Information("Running spv wallet");
            var registry = new Registry();
            registry.Schedule(() =>
            {
                if (_lastHeight != CurrentHeight)
                {
                    _logger.Information($"Connected nodes -> {ConnectedNodes.Count()}");
                    _logger.Information($"Height -> {CurrentHeight}");
                    _lastHeight = CurrentHeight;

                    _logger.Information($"Wallet Balance -> {GetBalance()}");
                    SaveAsync();
                    _logger.Information("******************************************");
                }

            }).ToRunNow().AndEvery(10).Seconds();
            JobManager.Initialize(registry);
            JobManager.Start();

            //Console.CancelKeyPress += (sender, eArgs) => {
            //    _quitEvent.Set();
            //    eArgs.Cancel = true;
            //};
            
            _wallet.Tracker.NewOperation += Tracker_NewOperation;
            // kick off asynchronous stuff 

            _quitEvent.WaitOne();


        }
        
        private void Tracker_NewOperation(Tracker sender, Tracker.IOperation trackerOperation)
        {
            _logger.Information("Tracker ...");
        }

        private void _wallet_NewWalletTransaction(Wallet sender, WalletTransaction walletTransaction)
        {
            _logger.Information("New wallet transaction received");
        }

        public decimal GetBalance()
        {
            return Transactions.Sum(x => x.Balance.ToUnit(MoneyUnit.BTC));
        }

        public WalletTransactionsCollection Transactions => _wallet.GetTransactions();


        private AddressManager GetAddressManager()
        {
            if (_connectionParameters != null)
            {
                return _connectionParameters.TemplateBehaviors.Find<AddressManagerBehavior>().AddressManager;
            }
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
            return Path.Combine(_settings.AppDir, "addrman.dat");
        }
        private ConcurrentChain GetChain()
        {
            if (_connectionParameters != null)
            {
                return _connectionParameters.TemplateBehaviors.Find<ChainBehavior>().Chain;
            }
            var chain = new ConcurrentChain(_settings.Network);
            try
            {
                lock (_saving)
                {
                    chain.Load(File.ReadAllBytes(ChainFile()));
                }
            }
            catch
            {
            }
            return chain;
        }

        private Tracker GetTracker()
        {
            if (_connectionParameters != null)
            {
                return _connectionParameters.TemplateBehaviors.Find<TrackerBehavior>().Tracker;
            }
            try
            {
                lock (_saving)
                {
                    using (var fs = File.OpenRead(TrackerFile()))
                    {
                        return Tracker.Load(fs);
                    }
                }
            }
            catch
            {
            }
            return new Tracker();
        }

        private string TrackerFile()
        {
            return Path.Combine(_settings.AppDir, "tracker.dat");
        }

        private string ChainFile()
        {
            return Path.Combine(_settings.AppDir, "chain.dat");
        }

        private async void PeriodicSave()
        {
            while (!_disposed)
            {
                await Task.Delay(100000);
                SaveAsync();
            }
        }
        private void SaveAsync()
        {
            _logger.Information("Saving ....");
            var unused = Task.Factory.StartNew(() =>
            {
                lock (_saving)
                {
                    GetAddressManager().SavePeerFile(AddrmanFile(), _settings.Network);
                    using (var fs = File.Open(ChainFile(), FileMode.Create))
                    {
                        GetChain().WriteTo(fs);
                    }
                    using (var fs = File.Open(TrackerFile(), FileMode.Create))
                    {
                        GetTracker().Save(fs);
                    }

                    SaveWallet();
                }
            });
        }

        private void LoadWallet()
        {
            var walletPath = WalletFile();
            using (var fs = File.Open(walletPath, FileMode.Open))
            {
                _wallet = Wallet.Load(fs);
            }
        }

        private async void PeriodicKick()
        {
            while (!_disposed)
            {
                await Task.Delay(TimeSpan.FromMinutes(10));
                _group.Purge("For privacy concerns, will renew bloom filters on fresh nodes");
            }
        }

        private void SaveWallet()
        {
            using (var fs = File.Open(WalletFile(), FileMode.Create))
            {
                _wallet.Save(fs);
            }
            //File.WriteAllText(PrivateKeyFile(), string.Join(",", PrivateKeys.AsEnumerable()));
        }

        private string WalletFile()
        {
            return Path.Combine(_settings.AppDir, "Wallet.dat");
        }

        private string PrivateKeyFile()
        {
            return Path.Combine(_settings.AppDir, "PrivateKeys");
        }

        public int CurrentHeight => GetChain().Height;

        public NodesCollection ConnectedNodes => _group.ConnectedNodes;

        public void Dispose()
        {
            _disposed = true;
            //SaveAsync();
            //if (_Group != null)
            //    _Group.Disconnect();

        }
    }
}
