using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace Files
{
    public class UWPHasher : IChecksum
    {
        private CryptographicHash hasher;

        public UWPHasher(string algorithm)
        {
            hasher = HashAlgorithmProvider.OpenAlgorithm(algorithm).CreateHash();
        }

        public async Task<byte[]> Finish()
        {
            return await Task.Run(() => {
                var buffer = hasher.GetValueAndReset();
                DataReader dataReader = DataReader.FromBuffer(buffer);
                byte[] bytes = new byte[buffer.Length];
                dataReader.ReadBytes(bytes);
                return bytes;
            });
        }

        public async void Update(IEnumerable<byte> buffer)
        {
            await Task.Run(() => hasher.Append(buffer.ToArray().AsBuffer()));
        }
    }
}
