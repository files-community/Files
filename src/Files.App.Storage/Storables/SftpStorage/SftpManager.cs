// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Net;

namespace Files.App.Storage.SftpStorage
{
	public static class SftpManager
	{
		public static readonly Dictionary<string, NetworkCredential> Credentials = [];

		public static readonly NetworkCredential EmptyCredentials = new(string.Empty, string.Empty);
	}
}
