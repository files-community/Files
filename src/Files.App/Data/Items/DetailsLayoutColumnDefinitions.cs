// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Data.Items
{
	/// <summary>
	/// Describes a built-in column of <see cref="DetailsLayoutPage"/>.
	/// </summary>
	/// <param name="Id">The column id.</param>
	/// <param name="HeaderResourceKey">The resource key of the header text.</param>
	/// <param name="IsGitColumn">Whether the column is only shown in Git repositories.</param>
	/// <param name="DefaultWidth">The default width in pixels.</param>
	/// <param name="IsVisibleByDefault">Whether the column is shown by default.</param>
	/// <param name="SortOption">The sort option the header sorts by, or <see langword="null"/> to sort by the column.</param>
	/// <param name="GetText">Gets the cell text, or <see langword="null"/> when the cell is defined in XAML.</param>
	/// <param name="TextProperties">The item properties the cell text depends on.</param>
	public sealed record DetailsLayoutColumnDefinition(
		string Id,
		string HeaderResourceKey,
		bool IsGitColumn,
		double DefaultWidth,
		bool IsVisibleByDefault,
		SortOption? SortOption = null,
		Func<ListedItem, string?>? GetText = null,
		string[]? TextProperties = null,
		double MinWidth = 50,
		double MaxWidth = 800,
		bool IsResizable = true);

	/// <summary>
	/// Provides the built-in columns of <see cref="DetailsLayoutPage"/>.
	/// </summary>
	public static class DetailsLayoutColumnDefinitions
	{
		public static IReadOnlyList<DetailsLayoutColumnDefinition> All { get; } =
		[
			new(DetailsLayoutColumnIds.Icon, string.Empty, false, 24, true, IsResizable: false),
			new(DetailsLayoutColumnIds.Name, Strings.Name, false, 240, true, SortOption.Name, MaxWidth: 1000),
			new(DetailsLayoutColumnIds.GitStatus, Strings.GitStatus, true, 80, false),
			new(DetailsLayoutColumnIds.GitLastCommitDate, Strings.DateCommitted, true, 140, false,
				GetText: item => (item as IGitItem)?.GitLastCommitDateHumanized, TextProperties: [nameof(IGitItem.GitLastCommitDateHumanized)]),
			new(DetailsLayoutColumnIds.GitLastCommitMessage, Strings.CommitMessage, true, 140, false,
				GetText: item => (item as IGitItem)?.GitLastCommitMessage, TextProperties: [nameof(IGitItem.GitLastCommitMessage)]),
			new(DetailsLayoutColumnIds.GitCommitAuthor, Strings.Author, true, 140, false,
				GetText: item => (item as IGitItem)?.GitLastCommitAuthor, TextProperties: [nameof(IGitItem.GitLastCommitAuthor)]),
			new(DetailsLayoutColumnIds.GitLastCommitSha, Strings.CommitSha, true, 80, false,
				GetText: item => (item as IGitItem)?.GitLastCommitSha, TextProperties: [nameof(IGitItem.GitLastCommitSha)]),
			new(DetailsLayoutColumnIds.Tag, Strings.Tag, false, 140, true, SortOption.FileTag),
			new(DetailsLayoutColumnIds.Path, Strings.Path, false, 200, true, SortOption.Path,
				item => item.ItemPath, [nameof(ListedItem.ItemPath)], MaxWidth: 500),
			new(DetailsLayoutColumnIds.OriginalPath, Strings.OriginalPath, false, 200, true, SortOption.OriginalFolder,
				item => (item as RecycleBinItem)?.ItemOriginalPath, [nameof(RecycleBinItem.ItemOriginalPath)], MaxWidth: 500),
			new(DetailsLayoutColumnIds.DateDeleted, Strings.DateDeleted, false, 200, true, SortOption.DateDeleted,
				item => (item as RecycleBinItem)?.ItemDateDeleted, [nameof(RecycleBinItem.ItemDateDeleted)]),
			new(DetailsLayoutColumnIds.DateModified, Strings.DateModifiedLowerCase, false, 200, true, SortOption.DateModified,
				item => item.ItemDateModified, [nameof(ListedItem.ItemDateModified)]),
			new(DetailsLayoutColumnIds.DateCreated, Strings.DateCreated, false, 200, false, SortOption.DateCreated,
				item => item.ItemDateCreated, [nameof(ListedItem.ItemDateCreated)]),
			new(DetailsLayoutColumnIds.ItemType, Strings.Type, false, 140, true, SortOption.FileType,
				item => item.ItemType, [nameof(ListedItem.ItemType)]),
			new(DetailsLayoutColumnIds.Size, Strings.Size, false, 100, true, SortOption.Size),
			new(DetailsLayoutColumnIds.Status, Strings.Status, false, 50, true, SortOption.SyncStatus, MaxWidth: 80),
		];

		private static readonly Dictionary<string, DetailsLayoutColumnDefinition> _definitions = All.ToDictionary(definition => definition.Id);

		public static DetailsLayoutColumnDefinition? Get(string? id)
		{
			return id is not null && _definitions.TryGetValue(id, out var definition) ? definition : null;
		}

		public static bool IsBuiltIn(string? id)
		{
			return id is not null && _definitions.ContainsKey(id);
		}

		public static string GetHeader(string id)
		{
			return Get(id) is { HeaderResourceKey.Length: > 0 } definition ? definition.HeaderResourceKey.GetLocalizedResource() : string.Empty;
		}
	}
}
