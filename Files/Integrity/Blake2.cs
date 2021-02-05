using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blake2Fast;
using Blake2Fast.Implementation;

namespace Files
{
    public class Blake2 : IChecksum
    {
        private Blake2bHashState hasher;

        public Blake2()
        {
            hasher = Blake2b.CreateIncrementalHasher();
        }

        public async Task<byte[]> Finish()
        {
            return await Task.Run(() => hasher.Finish());
        }

        public async void Update(IEnumerable<byte> buffer)
        {
            await Task.Run(() => hasher.Update(buffer.ToArray()));
        }
    }
}
