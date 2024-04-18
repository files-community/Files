// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared;

namespace Files.App.Server.Data
{
	[RegistrySerializable]
	public sealed class LayoutPreferences
	{
		public ulong? Frn { get; set; }
		public string FilePath { get; set; } = string.Empty;

		public LayoutPreferencesItem LayoutPreferencesManager { get; set; } = new();
	}
}
