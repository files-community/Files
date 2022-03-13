using System.Collections.Generic;
using System.Linq;

namespace Files.Backend.Item
{
    public static class ToDriveItemViewModelConverter
    {
        public static IDriveItemViewModel ToViewModel(this IDriveItem item) => new DriveItemViewModel(item);

        public static IEnumerable<IDriveItemViewModel> ToViewModel(this IEnumerable<IDriveItem> items)
            => items.Select(item => ToViewModel(item));

        public static async IAsyncEnumerable<IDriveItemViewModel> ToViewModel(this IAsyncEnumerable<IDriveItem> items)
        {
            await foreach (var item in items)
            {
                yield return ToViewModel(item);
            }
        }
    }
}
