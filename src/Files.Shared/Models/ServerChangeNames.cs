// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Shared;

public static class ServerChangeNames
{
	public const string PinnedFolders = "PinnedFolders";
	public const string RecentItems = "RecentItems";
	public const string Libraries = "Libraries";
	public const string Network = "Network";
	public const string RecycleBin = "RecycleBin";
	public const string Drives = "Drives";
	public const string Updates = "Updates";

	public static string Settings(string fileName) => $"Settings:{fileName}";
}
