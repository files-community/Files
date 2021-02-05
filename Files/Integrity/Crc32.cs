using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Force.Crc32;

namespace Files
{
    public class Crc32 : IChecksum
    {
        private uint hash = 0;

        public Crc32() {}

        public async Task<byte[]> Finish()
        {
            return await Task.Run(() => BitConverter.GetBytes(hash));
        }

        public async void Update(IEnumerable<byte> buffer)
        {
            hash = await Task.Run(() => Crc32Algorithm.Append(hash, buffer.ToArray()));
        }

    }
}
