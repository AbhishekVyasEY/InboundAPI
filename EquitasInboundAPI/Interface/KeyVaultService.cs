namespace EquitasInboundAPI
{
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Extensions.Configuration;
    public class KeyVaultService : IKeyVaultService
    {
        /// <summary>
        /// This function reads keys from from config
        /// </summary>
        /// <param name="key">This is the key</param>
        /// <returns>The value of the key from configuration</returns>
        public string ReadSecret(string key)
        {
            var MyConfig = new ConfigurationBuilder().AddJsonFile("data/config/appsettings.json").Build();
            string AppsettingValue = MyConfig.GetValue<string>(key);
            return AppsettingValue;
        }


        public static string getSecret(string key)
        {
            string AppsettingValue = new KeyVaultService().ReadSecret(key);
            return AppsettingValue;
        }

    }


}
