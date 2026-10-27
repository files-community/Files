// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Stable identifiers of the built-in columns shown in <see cref="DetailsLayoutPage"/>.
	/// </summary>
	/// <remarks>
	/// Columns that correspond to a Windows property use its canonical name, so they aren't offered twice.
	/// </remarks>
	public static class DetailsLayoutColumnIds
	{
		public const string Icon = "Icon";
		public const string Name = "System.ItemNameDisplay";
		public const string GitStatus = "Files.Git.Status";
		public const string GitLastCommitDate = "Files.Git.LastCommitDate";
		public const string GitLastCommitMessage = "Files.Git.LastCommitMessage";
		public const string GitCommitAuthor = "Files.Git.CommitAuthor";
		public const string GitLastCommitSha = "Files.Git.LastCommitSha";
		public const string Tag = "Files.Tags";
		public const string Path = "System.ItemPathDisplay";
		public const string OriginalPath = "System.Recycle.DeletedFrom";
		public const string DateDeleted = "System.Recycle.DateDeleted";
		public const string DateModified = "System.DateModified";
		public const string DateCreated = "System.DateCreated";
		public const string ItemType = "System.ItemTypeText";
		public const string Size = "System.Size";
		public const string Status = "System.StorageProviderUIStatus";

		/// <summary>
		/// Gets all built-in column ids in their default display order.
		/// </summary>
		public static IReadOnlyList<string> All { get; } = [.. DetailsLayoutColumnDefinitions.All.Select(definition => definition.Id)];
	}
}
