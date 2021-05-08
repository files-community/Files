using Files.Enums;
using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Files.Extensions;

namespace Files.Helpers
{
    public static class GroupingHelper
    {
        public static Func<ListedItem, string> GetItemGroupKeySelector(GroupOption option)
        {
            return option switch
            {
                GroupOption.Name => x => new string(x.ItemName.Take(1).ToArray()).ToUpper(),
                GroupOption.Size => x => x.PrimaryItemAttribute != StorageItemTypes.Folder ? GetGroupSizeString(x.FileSizeBytes) : x.FileSizeDisplay,
                GroupOption.DateCreated => x => x.ItemDateCreatedReal.GetFriendlyTimeSpan().text,
                GroupOption.DateModified => x => x.ItemDateModifiedReal.GetFriendlyTimeSpan().text,
                GroupOption.FileType => x => x.PrimaryItemAttribute == StorageItemTypes.Folder ? x.ItemType : x.FileExtension?.ToLower() ?? " ",
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

                _ => (null, null)
            };
        }

        public static List<GroupOptionListing> GetGroupOptionsMenuItems() => new List<GroupOptionListing>()
        {
            new GroupOptionListing()
            {
                GroupOption = GroupOption.None,
                Text = "None",
            },
            new GroupOptionListing()
            {
                GroupOption = GroupOption.Name,
                Text = "Name",
            },
            new GroupOptionListing()
            {
                GroupOption = GroupOption.Size,
                Text = "Size",
            },
            new GroupOptionListing()
            {
                GroupOption = GroupOption.FileType,
                Text = "File type",
            },
            new GroupOptionListing()
            {
                GroupOption = GroupOption.DateCreated,
                Text = "Date created",
            },
        };

        public static string GetGroupSizeString(long size)
        {
            if (size > 500000)
            {
                return "500000";
            }
            if (size > 10000)
            {
                return "10000";
            }
            return "0";
        }
    }

    public class GroupOptionListing
    {
        public string Text { get; set; }
        public GroupOption GroupOption { get; set; }
    }

    public interface IGroupableItem
    {
        public string Key { get; set; }
    }
}
