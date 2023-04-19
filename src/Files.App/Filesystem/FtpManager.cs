using System.Net;

namespace Files.App.Filesystem
{
	public static class FtpManager
	{
		public static Dictionary<string, NetworkCredential> Credentials = new();

		public static readonly NetworkCredential Anonymous = new("anonymous", "anonymous");
	}
}