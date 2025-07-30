// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Net;

namespace Files.App.Storage
{
	public static class FtpManager
	{
		public static readonly Dictionary<string, NetworkCredential> Credentials = [];

		public static readonly NetworkCredential Anonymous = new("anonymous", "anonymous");
	}
}
