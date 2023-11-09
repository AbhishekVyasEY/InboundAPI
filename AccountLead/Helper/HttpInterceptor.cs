namespace DigiLead
{
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Identity.Client;
    using Newtonsoft.Json;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public class HttpInterceptor : DelegatingHandler
    {
        /// <summary>
        /// initiates CacheExpiryTimeAdjustmentMinutes
        /// </summary>
        private const int CacheExpiryTimeAdjustmentMinutes = -5; //// change to -5 to say 5 min prior the oauth token expires

        /// <summary>
        /// initiates BearerHeaderValueCacheKey
        /// </summary>
        private const string BearerHeaderValueCacheKey = nameof(HttpInterceptor) + "-BearerValue";

        /// <summary>
        /// initiates _syncObject
        /// </summary>
        private static readonly object SyncObject = new object();

        /// <summary>
        /// Injects IMemoryCache using IoC
        /// </summary>
        private readonly IMemoryCache _cache;

        /// <summary>
        /// initiates KeyVault service to get configurations
        /// </summary>
        private readonly IKeyVaultService _keyVaultService;

        public HttpInterceptor(IMemoryCache cache, IKeyVaultService keyVaultService)
        {
            this._cache = cache;
            this._keyVaultService = keyVaultService;
        }


        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
           

            if (!this._cache.TryGetValue<string>(BearerHeaderValueCacheKey, out var bearerHeaderValue))
            {
                // lock all threads with sync
                lock (SyncObject)
                {
                    if (!this._cache.TryGetValue<string>(BearerHeaderValueCacheKey, out bearerHeaderValue))
                    {
                        // Get new token
                        bearerHeaderValue = this.AcquireNewTokenAsync(cancellationToken).GetAwaiter().GetResult();
                    }
                }
            }

            // Set the auth header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerHeaderValue);
            request.Headers.Add("MSCRM.BypassCustomPluginExceution", "true");

            var responceMessages = base.SendAsync(request, cancellationToken);
            //ErrorLogger.requestPerser(request, responceMessages.Result);
            return responceMessages;
        }


        private async Task<string> AcquireNewTokenAsync(CancellationToken cancellationToken)
        {
            var authority = $"https://login.microsoftonline.com/{this._keyVaultService.ReadSecret("TenantId")}";
            var app =
                ConfidentialClientApplicationBuilder.Create(
                    this._keyVaultService.ReadSecret("DynamicsClientId"))
                                                    .WithClientSecret(this._keyVaultService.ReadSecret("DynamicsSecretId"))
                                                    .WithAuthority(authority)
                                                    .Build();

            var authResult = await app.AcquireTokenForClient(new[] { this._keyVaultService.ReadSecret("DynamicsScope") })
                .ExecuteAsync(cancellationToken).ConfigureAwait(true);

            string bearerHeaderValue = authResult.AccessToken;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(authResult.ExpiresOn.AddMinutes(CacheExpiryTimeAdjustmentMinutes));

            this._cache.Set(BearerHeaderValueCacheKey, bearerHeaderValue, cacheEntryOptions);

            return bearerHeaderValue;
        }


    }
}
