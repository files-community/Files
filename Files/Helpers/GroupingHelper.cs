using Files.Enums;
using Files.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.Helpers
{
    public static class GroupingHelper
    {
        public static Func<ListedItem, string> GetItemGroupKeySelector(GroupOption option)
        {
            switch(option)
            {
                case GroupOption.Name:
                    return x => new string(x.ItemName.Take(1).ToArray()).ToUpper();
                
                case GroupOption.Size:
                    return x => x.FileSizeDisplay;

                case GroupOption.None:
                default:
                    return null;
            }
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
        };
    }

    public class GroupOptionListing
    {
        public string Text { get; set; }
        public GroupOption GroupOption { get; set; }
    }
}
