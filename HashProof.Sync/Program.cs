using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using HashProof.Core.Data;
using HashProof.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace HashProof.Sync
{
    public class Program
    {
        static readonly ManualResetEvent QuitEvent = new ManualResetEvent(false);

        public static void Main(string[] args)
        {

            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var config = configBuilder.Build();

            Log.Logger = new LoggerConfiguration()
                 .MinimumLevel.Debug()
                 .WriteTo.ColoredConsole()
                 .WriteTo.RollingFile(@"logs\hashproof-{Date}.txt")
                 .CreateLogger();

            var logger = Log.Logger;
            var appDir = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "data");
            logger.Information($"Application base path {appDir}");
            var privateKey = config["HashProof:Stratis:PrivateKey"];
           
            var connString = config["HashProof:ConnectionString"];
            var nodes = config["HashProof:Stratis:Nodes"];

            var services = new ServiceCollection();
            services.AddSingleton<IDataRepository>(new MongoDbRepository(connString));
            services.AddSingleton<IDataService>(new DataService());
            services.AddSingleton<IProofService>(new ProofService(privateKey, nodes));
            services.AddSingleton<Serilog.ILogger>(Log.Logger);

            var provider = services.BuildServiceProvider();
            DependencyResolver.Provider = provider;
            var settings = new SyncSettings
            {
                Logger = logger,
                Wallet = new WalletSettings
                {
                    MasterPrivKey = privateKey
                },
                AppDir = appDir,
                Provider = provider,
                Nodes = nodes
            };

            logger.Information("Initiasing sync module");
            var nodeSync = new NodeSync(settings);
            logger.Information("Initialising Scheduling ..");
            var registry = new Registry();
            registry.Schedule(() =>
            {
                if (!nodeSync.IsRunning)
                {
                    logger.Information("Starting sync ");
                    nodeSync.Sync();
                }

            }).ToRunNow().AndEvery(1).Minutes();

            JobManager.Initialize(registry);
            logger.Information("Starting initializing");
            JobManager.Start();


            QuitEvent.WaitOne();

        }
    }
}
