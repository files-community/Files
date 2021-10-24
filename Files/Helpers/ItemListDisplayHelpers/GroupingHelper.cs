using Files.Enums;
using Files.Extensions;
using Files.Filesystem;
using Microsoft.Toolkit.Uwp;
using System;
using System.Linq;
using Windows.Storage;

namespace Files.Helpers
{
    public static class GroupingHelper
    {
        public static Func<ListedItem, string> GetItemGroupKeySelector(GroupOption option)
        {
            return option switch
            {
                GroupOption.Name => x => new string(x.ItemName.Take(1).ToArray()).ToUpper(),
                GroupOption.Size => x => x.PrimaryItemAttribute != StorageItemTypes.Folder ? GetGroupSizeKey(x.FileSizeBytes) : x.FileSizeDisplay,
                GroupOption.DateCreated => x => x.ItemDateCreatedReal.GetUserSettingsFriendlyTimeSpan().text,
                GroupOption.DateModified => x => x.ItemDateModifiedReal.GetUserSettingsFriendlyTimeSpan().text,
                GroupOption.FileType => x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsShortcutItem ? x.ItemType : x.FileExtension?.ToLower() ?? " ",
                GroupOption.SyncStatus => x => x.SyncStatusString,
                GroupOption.FileTag => x => x.FileTag,
                GroupOption.OriginalFolder => x => (x as RecycleBinItem)?.ItemOriginalFolder,
                GroupOption.DateDeleted => x => (x as RecycleBinItem)?.ItemDateDeletedReal.GetUserSettingsFriendlyTimeSpan().text,
                GroupOption.FolderPath => x => PathNormalization.GetParentDir(x.ItemPath.TrimPath()),
                _ => null,
            };
        }

        public static (Action<GroupedCollection<ListedItem>>, Action<GroupedCollection<ListedItem>>) GetGroupInfoSelector(GroupOption option)
        {
            return option switch
            {
                GroupOption.FileType => (x =>
                {
                    var first = x.First();
                    x.Model.Text = first.ItemType;
                    x.Model.Subtext = first.FileExtension;
                    if (first.IsShortcutItem)
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
                    if (first.PrimaryItemAttribute != StorageItemTypes.Folder)
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
                    var vals = x.First().ItemDateCreatedReal.GetUserSettingsFriendlyTimeSpan();
                    x.Model.Subtext = vals.range;
                    x.Model.Icon = vals.glyph;
                    x.Model.SortIndexOverride = vals.index;
                }, null),
                GroupOption.DateModified => (x =>
                    {
                        var vals = x.First().ItemDateModifiedReal.GetUserSettingsFriendlyTimeSpan();
                        x.Model.Subtext = vals.range;
                        x.Model.Icon = vals.glyph;
                        x.Model.SortIndexOverride = vals.index;
                    }, null),

                GroupOption.SyncStatus => (x =>
                {
                    ListedItem first = x.First();
                    x.Model.ShowCountTextBelow = true;
                    x.Model.Text = first.SyncStatusString;
                    x.Model.Icon = first?.SyncStatusUI.Glyph;
                }, null),

                GroupOption.FileTag => (x =>
                {
                    ListedItem first = x.First();
                    x.Model.ShowCountTextBelow = true;
                    x.Model.Text = first.FileTagUI?.TagName ?? "NavToolbarGroupByOptionNone/Text".GetLocalized();
                    //x.Model.Icon = first?.FileTagUI?.Color;
                }, null),

                GroupOption.DateDeleted => (x =>
                    {
                        var vals = (x.First() as RecycleBinItem)?.ItemDateDeletedReal.GetUserSettingsFriendlyTimeSpan() ?? null;
                        x.Model.Subtext = vals?.range;
                        x.Model.Icon = vals?.glyph;
                        x.Model.SortIndexOverride = vals?.index ?? 0;
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
                    var parentPath = PathNormalization.GetParentDir(first.ItemPath.TrimPath());
                    model.Text = System.IO.Path.GetFileName(parentPath);
                    model.Subtext = parentPath;
                }, null),
                _ => (null, null)
            };
        }

        public static (string key, string text, string range, int index) GetGroupSizeInfo(long size)
        {
            string lastSizeStr = default;
            for (int i = 0; i < sizeGroups.Length; i++)
            {
                var sizeGp = sizeGroups[i];
                if (size > sizeGp.size)
                {
                    var rangeStr = i > 0 ? $"{sizeGp.sizeText} - {sizeGroups[i - 1].sizeText}" : $"{sizeGp.sizeText} +";
                    return (sizeGp.size.ToString(), sizeGp.text, rangeStr, i + 1); //i +1 is so that other groups always show below "unspecified"
                }
                lastSizeStr = sizeGp.sizeText;
            }

            return ("0", "ItemSizeText_Tiny".GetLocalized(), $"{"0 B".ConvertSizeAbbreviation()} - {lastSizeStr}", sizeGroups.Length + 1);
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

        private static readonly (long size, string text, string sizeText)[] sizeGroups = new (long, string, string)[]
        {
            (5000000000, "ItemSizeText_Huge".GetLocalized(), "5 GiB".ConvertSizeAbbreviation()),
            (1000000000, "ItemSizeText_VeryLarge".GetLocalized(), "1 GiB".ConvertSizeAbbreviation()),
            (128000000, "ItemSizeText_Large".GetLocalized(), "128 MiB".ConvertSizeAbbreviation()),
            (1000000, "ItemSizeText_Medium".GetLocalized(), "1 MiB".ConvertSizeAbbreviation()),
            (16000, "ItemSizeText_Small".GetLocalized(), "16 KiB".ConvertSizeAbbreviation()),
        };
    }

    public interface IGroupableItem
    {
        public string Key { get; set; }
    }
}