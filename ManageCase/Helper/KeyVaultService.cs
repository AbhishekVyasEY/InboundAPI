namespace ManageCase
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Configuration.AzureKeyVault;
    using Microsoft.Extensions.Caching.Memory;
    using System.Threading;

    public class KeyVaultService : IKeyVaultService
    {
        private readonly IConfigurationBuilder configuration_builder;
        private readonly IMemoryCache _cache;
        private static readonly object SyncObject = new object();



        public KeyVaultService(IMemoryCache cache)
        {
            this._cache = cache;
            this.configuration_builder = new ConfigurationBuilder();
        }
        /// <summary>
        /// This function reads keys from from config
        /// </summary>
        /// <param name="key">This is the key</param>
        /// <returns>The value of the key from configuration</returns>
       

        public string ReadSecret(string key)
        {
           
            if (!this._cache.TryGetValue<string>(key, out var AppsettingValue))
            {
                // lock all threads with sync
                lock (SyncObject)
                {
                    if (!this._cache.TryGetValue<string>(key, out AppsettingValue))
                    {
                        // Get new token
                        var MyConfig = this.configuration_builder.AddJsonFile("data/config/appsettings.json").Build();
                        var keyVaultURL = MyConfig["KeyVaultConfiguration:KeyVaultURL"];
                        var keyVaultClientId = MyConfig["KeyVaultConfiguration:ClientId"];
                        var keyVaultClientSecret = MyConfig["KeyVaultConfiguration:ClientSecret"];
                        var MyKConfig = this.configuration_builder.AddAzureKeyVault(keyVaultURL, keyVaultClientId, keyVaultClientSecret, new DefaultKeyVaultSecretManager()).Build();
                                                 
                        AppsettingValue = MyKConfig.GetValue<string>(key);

                        var cacheEntryOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(DateTime.Now.AddDays(1));

                        this._cache.Set(key, AppsettingValue, cacheEntryOptions);
                    }
                }
            }
            return AppsettingValue;
        }


        

    }


}
