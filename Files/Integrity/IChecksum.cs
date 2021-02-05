using System.Collections.Generic;
using System.Threading.Tasks;

namespace Files
{
    public interface IChecksum
    {
        public void Update(IEnumerable<byte> buffer);
        public Task<byte[]> Finish();
    }
}
