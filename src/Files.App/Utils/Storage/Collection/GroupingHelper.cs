// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;

namespace Files.App.Utils.Storage
{
	public static class GroupingHelper
	{
		private const string RatingId = "System.Rating";

		/// <summary>
		/// Gets the sort index of the group of items without a value when grouping by a property, which stays last in either direction.
		/// </summary>
		public const int EmptyPropertyGroupIndex = int.MaxValue;

		private static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetRequiredService<IDateTimeFormatter>();

		public static Func<ListedItem, string?>? GetItemGroupKeySelector(GroupOption option, GroupByDateUnit unit, string? propertyId = null)
		{
			return option switch
			{
				GroupOption.Name => x => new string((x.Name
					?? throw new InvalidOperationException("The grouped item does not have a name."))
					.Take(1).ToArray()).ToUpperInvariant(),
				GroupOption.Size => x => x.PrimaryItemAttribute != StorageItemTypes.Folder || x.IsArchive ? GetGroupSizeKey(x.FileSizeBytes) : x.FileSizeDisplay,
				GroupOption.DateCreated => x => dateTimeFormatter.ToTimeSpanLabel(x.ItemDateCreatedReal, unit).Text,
				GroupOption.DateModified => x => dateTimeFormatter.ToTimeSpanLabel(x.ItemDateModifiedReal, unit).Text,
				GroupOption.FileType => x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsShortcut ? x.ItemType : x.FileExtension?.ToLowerInvariant() ?? " ",
				GroupOption.SyncStatus => x => x.SyncStatusString,
				GroupOption.FileTag => x => x.FileTags?.FirstOrDefault() ?? "Untagged",
				GroupOption.OriginalFolder => x => (x as RecycleBinItem)?.ItemOriginalFolder,
				GroupOption.DateDeleted => x => dateTimeFormatter.ToTimeSpanLabel((x as RecycleBinItem)?.ItemDateDeletedReal ?? DateTimeOffset.Now, unit).Text,
				GroupOption.FolderPath => x => PathNormalization.GetParentDir(x.GetRequiredPath().TrimPath()),
				GroupOption.Property when !string.IsNullOrEmpty(propertyId) => x => GetPropertyGroupKey(x, propertyId, unit),
				_ => null,
			};
		}

		public static (Action<GroupedCollection<ListedItem>>?, Action<GroupedCollection<ListedItem>>?) GetGroupInfoSelector(GroupOption option, GroupByDateUnit unit, string? propertyId = null)
		{
			return option switch
			{
				GroupOption.FileType => (x =>
				{
					var first = x.First();
					x.Model.Text = first.ItemType;
					x.Model.Subtext = first.FileExtension;
					if (first.IsShortcut)
					{
						x.Model.Icon = "\uE71B";
					}
					if (first.PrimaryItemAttribute != StorageItemTypes.Folder)
					{
						// Always show file sections below folders
						x.Model.SortIndexOverride = 1;
					}
				}, x =>
				{
					ListedItem first = x.First();
					var model = x.Model;

					model.Text = first.ItemType;
				}
				),
				GroupOption.Size => (x =>
				{
					var first = x.First();
					if (first.PrimaryItemAttribute != StorageItemTypes.Folder || first.IsArchive)
					{
						var vals = GetGroupSizeInfo(first.FileSizeBytes);
						//x.Model.Text = vals.text;
						x.Model.Subtext = vals.range;
						x.Model.Text = vals.range;
						x.Model.SortIndexOverride = vals.index;
					}
				}, null),
				GroupOption.DateCreated => (x =>
				{
					var vals = dateTimeFormatter.ToTimeSpanLabel(x.First().ItemDateCreatedReal, unit);
					x.Model.Subtext = vals.Text;
					x.Model.Icon = vals.Glyph;
					x.Model.SortIndexOverride = vals.Index;
				}, null),
				GroupOption.DateModified => (x =>
					{
						var vals = dateTimeFormatter.ToTimeSpanLabel(x.First().ItemDateModifiedReal, unit);
						x.Model.Subtext = vals.Text;
						x.Model.Icon = vals.Glyph;
						x.Model.SortIndexOverride = vals.Index;
					}, null),

				GroupOption.SyncStatus => (x =>
				{
					ListedItem first = x.First();
					x.Model.ShowCountTextBelow = true;
					x.Model.Text = first.SyncStatusString;
					x.Model.Icon = first.SyncStatusUI.Glyph;
				}, null),

				GroupOption.FileTag => (x =>
				{
					ListedItem first = x.First();
					x.Model.ShowCountTextBelow = true;
					x.Model.Text = first.FileTagsUI?.FirstOrDefault()?.Name ?? Strings.Untagged.GetLocalizedResource();
					//x.Model.Icon = first.FileTagsUI?.FirstOrDefault()?.Color;
				}, null),

				GroupOption.DateDeleted => (x =>
					{
						var vals = dateTimeFormatter.ToTimeSpanLabel((x.First() as RecycleBinItem)?.ItemDateDeletedReal ?? DateTimeOffset.Now, unit);
						x.Model.Subtext = vals.Text;
						x.Model.Icon = vals.Glyph;
						x.Model.SortIndexOverride = vals.Index;
					}, null),

				GroupOption.OriginalFolder => (x =>
					{
						ListedItem first = x.First();
						var model = x.Model;
						model.ShowCountTextBelow = true;

						model.Text = (first as RecycleBinItem)?.ItemOriginalFolderName;
						model.Subtext = (first as RecycleBinItem)?.ItemOriginalFolder;
					}, null),

				GroupOption.FolderPath => (x =>
				{
					ListedItem first = x.First();
					var model = x.Model;
					model.ShowCountTextBelow = true;
					var parentPath = PathNormalization.GetParentDir(first.GetRequiredPath().TrimPath());
					model.Text = GetFolderName(parentPath);
					model.Subtext = parentPath;
				}, null),
				GroupOption.Property when !string.IsNullOrEmpty(propertyId) => (x =>
				{
					var model = x.Model;
					model.ShowCountTextBelow = true;
					if (ColumnValueHelper.GetValue(x.First(), propertyId) is DateTimeOffset date)
					{
						var vals = dateTimeFormatter.ToTimeSpanLabel(date, unit);
						model.Subtext = vals.Text;
						model.Icon = vals.Glyph;
						model.SortIndexOverride = vals.Index;
					}
					else
					{
						var value = ColumnValueHelper.GetValue(x.First(), propertyId);
						model.Text = GetPropertyGroupKey(x.First(), propertyId, unit);
						if (IsEmptyPropertyValue(value, propertyId))
							model.SortIndexOverride = EmptyPropertyGroupIndex;
						else if (value is byte or sbyte or short or ushort or int or uint or long)
							model.SortIndexOverride = (int)Math.Clamp(Convert.ToInt64(value), int.MinValue, EmptyPropertyGroupIndex - 1);
					}
				}, null),
				_ => (null, null)
			};
		}

		private static string GetPropertyGroupKey(ListedItem item, string propertyId, GroupByDateUnit unit)
		{
			var value = ColumnValueHelper.GetValue(item, propertyId);
			if (IsEmptyPropertyValue(value, propertyId))
				return propertyId == RatingId ? Strings.Unrated.GetLocalizedResource() : Strings.Unspecified.GetLocalizedResource();

			if (value is DateTimeOffset date)
				return dateTimeFormatter.ToTimeSpanLabel(date, unit).Text;

			return ColumnValueHelper.GetText(item, propertyId) is { Length: > 0 } text ? text : value!.ToString() ?? string.Empty;
		}

		private static bool IsEmptyPropertyValue(object? value, string propertyId)
		{
			return ColumnValueHelper.IsEmpty(value) || propertyId == RatingId && value is 0u;
		}

		public static (string key, string text, string range, int index) GetGroupSizeInfo(long size)
		{
			string lastSizeStr = string.Empty;
			for (int i = 0; i < sizeGroups.Length; i++)
			{
				var sizeGp = sizeGroups[i];
				if (size > sizeGp.size)
				{
					var rangeStr = i > 0 ? $"{sizeGp.sizeText} - {sizeGroups[i - 1].sizeText}" : $"{sizeGp.sizeText} +";
					return (sizeGp.size.ToString(), sizeGp.text, rangeStr, sizeGroups.Length - i);
				}
				lastSizeStr = sizeGp.sizeText;
			}

			return ("0", Strings.ItemSizeText_Tiny.GetLocalizedResource(), $"{"0 B".ConvertSizeAbbreviation()} - {lastSizeStr}", 0);
		}

		public static string GetGroupSizeKey(long size)
		{
			for (int i = 0; i < sizeGroups.Length; i++)
			{
				var sizeGp = sizeGroups[i];
				if (size > sizeGp.size)
				{
					return sizeGp.size.ToString();
				}
			}
			return "0";
		}

		private static readonly (long size, string text, string sizeText)[] sizeGroups =
		[
			(5000000000, Strings.ItemSizeText_Huge.GetLocalizedResource(), "5 GiB".ConvertSizeAbbreviation()),
			(1000000000, Strings.ItemSizeText_VeryLarge.GetLocalizedResource(), "1 GiB".ConvertSizeAbbreviation()),
			(128000000, Strings.ItemSizeText_Large.GetLocalizedResource(), "128 MiB".ConvertSizeAbbreviation()),
			(1000000, Strings.ItemSizeText_Medium.GetLocalizedResource(), "1 MiB".ConvertSizeAbbreviation()),
			(16000, Strings.ItemSizeText_Small.GetLocalizedResource(), "16 KiB".ConvertSizeAbbreviation()),
		];

		private static string? GetFolderName(string? path)
		{
			if (path is null)
				return null;

			return path.Substring(path.LastIndexOf('\\') + 1);
		}
	}
}
