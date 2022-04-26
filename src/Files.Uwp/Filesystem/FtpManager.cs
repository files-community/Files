using System.Collections.Generic;
using System.Net;

namespace Files.Uwp.Filesystem
{
    public static class FtpManager
    {
        public static Dictionary<string, NetworkCredential> Credentials = new Dictionary<string, NetworkCredential>();

        public static readonly NetworkCredential Anonymous = new NetworkCredential("anonymous", "anonymous");
    }
}