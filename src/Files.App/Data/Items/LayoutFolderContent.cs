// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents what a folder contains, as used by file type rules.
	/// </summary>
	/// <param name="Total">The number of items that were counted.</param>
	/// <param name="ExtensionCounts">The number of files with each extension.</param>
	public sealed record LayoutFolderContent(int Total, IReadOnlyDictionary<string, int> ExtensionCounts);
}
