// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;

namespace Files.App.Server
{
	[RegistrySerializable]
	internal sealed class TaggedFile
	{
		public ulong? Frn { get; set; }
		public string FilePath { get; set; } = string.Empty;
		public string[] Tags { get; set; } = [];
	}
}
