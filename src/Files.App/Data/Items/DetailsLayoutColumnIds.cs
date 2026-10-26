// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Stable identifiers of the columns shown in <see cref="DetailsLayoutPage"/>.
	/// </summary>
	public static class DetailsLayoutColumnIds
	{
		public const string Icon = "Icon";
		public const string Name = "Name";
		public const string GitStatus = "GitStatus";
		public const string GitLastCommitDate = "GitLastCommitDate";
		public const string GitLastCommitMessage = "GitLastCommitMessage";
		public const string GitCommitAuthor = "GitCommitAuthor";
		public const string GitLastCommitSha = "GitLastCommitSha";
		public const string Tag = "Tag";
		public const string Path = "Path";
		public const string OriginalPath = "OriginalPath";
		public const string DateDeleted = "DateDeleted";
		public const string DateModified = "DateModified";
		public const string DateCreated = "DateCreated";
		public const string ItemType = "ItemType";
		public const string Size = "Size";
		public const string Status = "Status";

		/// <summary>
		/// Gets all column ids in their default display order.
		/// </summary>
		public static IReadOnlyList<string> All { get; } =
		[
			Icon,
			Name,
			GitStatus,
			GitLastCommitDate,
			GitLastCommitMessage,
			GitCommitAuthor,
			GitLastCommitSha,
			Tag,
			Path,
			OriginalPath,
			DateDeleted,
			DateModified,
			DateCreated,
			ItemType,
			Size,
			Status,
		];
	}
}
