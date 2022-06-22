using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files.Shared.Services
{
    public interface IFullTrustAsker
    {
        Task<IFullTrustResponse> GetResponseAsync(IDictionary<string, object> parameters);
    }
}
