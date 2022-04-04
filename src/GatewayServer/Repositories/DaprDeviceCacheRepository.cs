using Dapr.Client;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Repositories
{
    public class DaprDeviceCacheRepository : IDistributedCache
    {
        private const string CACHE_STORE_NAME = "devicestatestore";
        private DaprClient daprClient;

        public DaprDeviceCacheRepository(DaprClient daprClient)
        {
            this.daprClient = daprClient;
        }

        public byte[] Get(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            return daprClient.GetStateAsync<byte[]>(CACHE_STORE_NAME, key).GetAwaiter().GetResult();
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            return await daprClient.GetStateAsync<byte[]>(CACHE_STORE_NAME, key);
        }

        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            if(key == null) 
                throw new ArgumentNullException(nameof(key));
            (daprClient.DeleteStateAsync(CACHE_STORE_NAME, key)).Wait();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            await daprClient.DeleteStateAsync(CACHE_STORE_NAME, key);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            (daprClient.SaveStateAsync<byte[]>(CACHE_STORE_NAME, key, value)).Wait();
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            await daprClient.SaveStateAsync<byte[]>(CACHE_STORE_NAME, key, value);
        }
    }
}
