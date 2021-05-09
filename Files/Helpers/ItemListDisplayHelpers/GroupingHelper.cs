using Files.Enums;
using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Files.Extensions;
using ByteSizeLib;
using Microsoft.Toolkit.Uwp;

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
                GroupOption.DateCreated => x => x.ItemDateCreatedReal.GetFriendlyTimeSpan().text,
                GroupOption.DateModified => x => x.ItemDateModifiedReal.GetFriendlyTimeSpan().text,
                GroupOption.FileType => x => x.PrimaryItemAttribute == StorageItemTypes.Folder ? x.ItemType : x.FileExtension?.ToLower() ?? " ",
                GroupOption.OriginalFolder => x => (x as RecycleBinItem)?.ItemOriginalFolder,
                GroupOption.DateDeleted => x => (x as RecycleBinItem)?.ItemDateDeletedReal.GetFriendlyTimeSpan().text,
                _ => null,
            };
        }

        public static (Action<GroupedCollection<ListedItem>>, Action<GroupedCollection<ListedItem>>) GetGroupInfoSelector(GroupOption option)
        {
            return option switch
            {
                GroupOption.FileType => (x =>
                {
                    x.Model.Subtext = x.Model.Key;
                    var first = x.First();
                    x.Model.Text = first.ItemType;
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
                    model.Subtext = first.FileExtension;
                }
                ),
                GroupOption.Size => (x =>
                {
                    var first = x.First();
                    if(first.PrimaryItemAttribute != StorageItemTypes.Folder)
                    {
                        var vals = GetGroupSizeInfo(first.FileSizeBytes);
                        x.Model.Text = vals.text;
                        x.Model.Subtext = vals.range;
                        x.Model.SortIndexOverride = vals.index;
                    }
                }, null),
                GroupOption.DateCreated => (x =>
                {
                    var vals = x.First().ItemDateCreatedReal.GetFriendlyTimeSpan();
                    x.Model.Subtext = vals.range;
                    x.Model.Icon = vals.glyph;
                    x.Model.SortIndexOverride = vals.index;
                }, null),
                GroupOption.DateModified => (x =>
                    {
                        var vals = x.First().ItemDateModifiedReal.GetFriendlyTimeSpan();
                        x.Model.Subtext = vals.range;
                        x.Model.Icon = vals.glyph;
                        x.Model.SortIndexOverride = vals.index;
                    }, null),
                
                GroupOption.DateDeleted => (x =>
                    {
                        var vals = (x.First() as RecycleBinItem)?.ItemDateDeletedReal.GetFriendlyTimeSpan() ?? null;
                        x.Model.Subtext = vals?.range;
                        x.Model.Icon = vals?.glyph;
                        x.Model.SortIndexOverride = vals?.index ?? 0;
                    }, null),
                    
                GroupOption.OriginalFolder => (x =>
                    {
                        ListedItem first = x.First();
                        var model = x.Model;

                        model.Text = (first as RecycleBinItem)?.ItemOriginalFolderName;
                        model.Subtext = (first as RecycleBinItem)?.ItemOriginalFolder;
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
                    return (sizeGp.size.ToString(), sizeGp.text, rangeStr, i+1); //i +1 is so that other groups always show below "unspecified"
                }
                lastSizeStr = sizeGp.sizeText;
            }

            return ("0", "Tiny", $"{"0 B".ConvertSizeAbbreviation()} - {lastSizeStr}", sizeGroups.Length+1);
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
            (5000000000, "Huge", "5 GiB".ConvertSizeAbbreviation()),
            (1000000000, "Very large", "1 GiB".ConvertSizeAbbreviation()), 
            (128000000, "Large", "128 MiB".ConvertSizeAbbreviation()), 
            (1000000, "Medium", "1 MiB".ConvertSizeAbbreviation()), // 1MB
            (16000, "Small", "16 KiB".ConvertSizeAbbreviation()), // 16kb
        };
    }

    public interface IGroupableItem
    {
        public string Key { get; set; }
    }
}
