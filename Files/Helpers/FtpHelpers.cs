using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public class FtpHelpers
    {
        public static bool IsFtpPath(string path)
        {
            return path.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) 
                || path.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetFtpHost(string path)
        {
            path = path.Replace("\\", "/");
            var schemaIndex = path.IndexOf("://") + 3;
            var hostIndex = path.IndexOf("/", schemaIndex);
            return hostIndex == -1 ? path : path.Substring(0, hostIndex);
        }

        public static string GetFtpPath(string path)
        {
            path = path.Replace("\\", "/");
            var schemaIndex = path.IndexOf("://") + 3;
            var hostIndex = path.IndexOf("/", schemaIndex);
            return hostIndex == -1 ? "/" : path.Substring(hostIndex);
        }

        public static string GetFtpDirectoryName(string path)
        {
            return System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        }
    }
}
