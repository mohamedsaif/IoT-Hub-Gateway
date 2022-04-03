using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Repositories
{
    public interface ICacheRepository<T>
    {
        Task SaveStateAsync(T state);
        Task<T?> GetStateAsync(string key);
    }
}
