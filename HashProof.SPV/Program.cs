using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nStratis;
using nStratis.BIP32;
using Serilog;

namespace HashProof.SPV
{
    public class Program
    {
        public static void Main(string[] args)
        {
         
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.ColoredConsole()
                .WriteTo.RollingFile("logs\\hashproof-{Date}.txt")
                .CreateLogger();

            var loggerFactory = new LoggerFactory()
             .AddSerilog();

            //var services = new ServiceCollection();
            //services.AddSingleton(loggerFactory);
            //services.AddSingleton(loggerFactory.CreateLogger("HashProof"));
            
            var spvWallet = new SPVWallet(new SPVSettings
            {
              Network  = Network.Main,
              AppDir = @"c:\data\",
              Logger = Log.Logger,
              RootKey = new BitcoinExtPubKey("xpub661MyMwAqRbcEeUgKjFNYkZA1nQyX4BE8o4EL2QBhTNhZ5C3scb2FRfPZgFmig1p9FpF3GTRcS9e9rUxwEcVBUHZPeJx9FFzRzTanVPCKCS")
            });

            spvWallet.Run();

        }
    }
}
