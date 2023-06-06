using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KeyVaultReader
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>

            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {                    
                    var builtConfig = config.Build();
                    var keyVaultEndpoint = "https://" + builtConfig["KeyVaultName"] + ".vault.azure.net";
                   // var connectionString = "RunAs=App;AppId={ClientId};TenantId={TenantId};AppKey={ClientSecret}";
                    var connectionString = "RunAs=App;AppId=3404dd26-4637-46f2-9008-fa6d84783d8d;TenantId=e22e4eaa-623f-4164-abb8-ac89a5a17e13;AppKey=0K08Q~UJzSop85IVavjd4~4Cqt9IPvdUmOyMrczR";
                    var azureServiceTokenProvider = new AzureServiceTokenProvider(connectionString);
                    var keyVaultClient = new KeyVaultClient(
                        new KeyVaultClient.AuthenticationCallback(
                            azureServiceTokenProvider.KeyVaultTokenCallback));
                    config.AddAzureKeyVault(keyVaultEndpoint, keyVaultClient, new DefaultKeyVaultSecretManager());

                   
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        
    }
}
