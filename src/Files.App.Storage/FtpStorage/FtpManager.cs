using System.Collections.Generic;
using System.Net;

namespace Files.App.Storage.FtpStorage
{
	internal static class FtpManager
	{
		public static readonly NetworkCredential Anonymous = new("anonymous", "anonymous");

		public static readonly IDictionary<string, NetworkCredential> Credentials = new Dictionary<string, NetworkCredential>();
	}
}
