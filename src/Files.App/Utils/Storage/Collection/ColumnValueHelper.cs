// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Provides the values of <see cref="DetailsLayoutPage"/> columns used for sorting and grouping.
	/// </summary>
	public static class ColumnValueHelper
	{
		public static bool IsShellPropertyColumn(string? columnId)
		{
			return columnId is not null && columnId.Contains('.');
		}

		public static bool IsGitColumn(string? columnId)
		{
			return columnId is DetailsLayoutColumnIds.GitStatus
				or DetailsLayoutColumnIds.GitLastCommitDate
				or DetailsLayoutColumnIds.GitLastCommitMessage
				or DetailsLayoutColumnIds.GitCommitAuthor
				or DetailsLayoutColumnIds.GitLastCommitSha;
		}

		public static object? GetValue(ListedItem item, string columnId)
		{
			return columnId switch
			{
				DetailsLayoutColumnIds.Name => item.Name,
				DetailsLayoutColumnIds.DateModified => item.ItemDateModifiedReal,
				DetailsLayoutColumnIds.DateCreated => item.ItemDateCreatedReal,
				DetailsLayoutColumnIds.ItemType => item.ItemType,
				DetailsLayoutColumnIds.Size => item.FileSizeBytes,
				DetailsLayoutColumnIds.Status => item.SyncStatusString,
				DetailsLayoutColumnIds.Tag => item.FileTags?.FirstOrDefault(),
				DetailsLayoutColumnIds.Path => item.ItemPath,
				DetailsLayoutColumnIds.OriginalPath => (item as RecycleBinItem)?.ItemOriginalFolder,
				DetailsLayoutColumnIds.DateDeleted => (item as RecycleBinItem)?.ItemDateDeletedReal,
				DetailsLayoutColumnIds.GitStatus => (item as IGitItem)?.UnmergedGitStatusName,
				DetailsLayoutColumnIds.GitLastCommitDate => (item as IGitItem)?.GitLastCommitDate,
				DetailsLayoutColumnIds.GitLastCommitMessage => (item as IGitItem)?.GitLastCommitMessage,
				DetailsLayoutColumnIds.GitCommitAuthor => (item as IGitItem)?.GitLastCommitAuthor,
				DetailsLayoutColumnIds.GitLastCommitSha => (item as IGitItem)?.GitLastCommitSha,
				_ => item.ShellPropertyValues is { } values && values.TryGetValue(columnId, out var value) ? value.Value ?? value.Text : null,
			};
		}

		public static string? GetText(ListedItem item, string columnId)
		{
			if (IsShellPropertyColumn(columnId))
				return item.ShellPropertyValues is { } values && values.TryGetValue(columnId, out var value) ? value.Text : null;

			return columnId switch
			{
				DetailsLayoutColumnIds.DateModified => item.ItemDateModified,
				DetailsLayoutColumnIds.DateCreated => item.ItemDateCreated,
				DetailsLayoutColumnIds.Size => item.FileSize,
				DetailsLayoutColumnIds.GitLastCommitDate => (item as IGitItem)?.GitLastCommitDateHumanized,
				DetailsLayoutColumnIds.DateDeleted => (item as RecycleBinItem)?.ItemDateDeleted,
				_ => GetValue(item, columnId)?.ToString(),
			};
		}

		public static bool IsEmpty(object? value)
		{
			return value is null || value is string text && string.IsNullOrEmpty(text);
		}
	}

	/// <summary>
	/// Compares column values of mixed types, placing text in natural order.
	/// </summary>
	public sealed class ColumnValueComparer(IComparer<object?> textComparer) : IComparer<object?>
	{
		public int Compare(object? x, object? y)
		{
			if (ReferenceEquals(x, y))
				return 0;
			if (x is null)
				return -1;
			if (y is null)
				return 1;

			if (x is string xText && y is string yText)
				return textComparer.Compare(xText, yText);

			if (IsNumber(x) && IsNumber(y))
				return Convert.ToDouble(x).CompareTo(Convert.ToDouble(y));

			if (x.GetType() == y.GetType() && x is IComparable comparable)
				return comparable.CompareTo(y);

			return textComparer.Compare(x.ToString(), y.ToString());
		}

		private static bool IsNumber(object value)
		{
			return value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
		}
	}
}
