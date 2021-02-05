using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files
{
    public class Blake3 : IChecksum
    {
        private Blake3_Arctium.BLAKE3 hasher;

        public Blake3()
        {
            hasher = new Blake3_Arctium.BLAKE3();
        }

        public async Task<byte[]> Finish()
        {
            return await Task.Run(() => hasher.HashFinal());
        }

        public async void Update(IEnumerable<byte> buffer)
        {
            await Task.Run(() => hasher.HashBytes(buffer.ToArray()));
        }
    }
}
