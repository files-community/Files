// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;

namespace Files.Storage
{
	[RegistrySerializable]
	public sealed class TaggedFile
	{
		public ulong? Frn { get; set; }
		public string FilePath { get; set; } = string.Empty;
		public string[] Tags { get; set; } = [];
	}
}
