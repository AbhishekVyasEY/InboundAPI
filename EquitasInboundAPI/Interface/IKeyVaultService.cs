namespace EquitasInboundAPI
{
    public interface IKeyVaultService
    {
        /// <summary>
        /// This function reads keys from from config
        /// </summary>
        /// <param name="key">This is the key</param>
        /// <returns>The value of the key from configuration</returns>
        public string ReadSecret(string key);
    }


}
