// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using LiteDB;

namespace Files.App.Server.Data
{
	public sealed class TaggedFile
	{
		[BsonId] public int Id { get; set; }
		public ulong? Frn { get; set; }
		public string FilePath { get; set; } = string.Empty;
		public string[] Tags { get; set; } = [];
	}
}
