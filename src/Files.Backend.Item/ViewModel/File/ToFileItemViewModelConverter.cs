using System.Collections.Generic;
using System.Linq;

namespace Files.Backend.Item
{
    public static class ToFileItemViewModelConverter
    {
        public static IFileItemViewModel ToViewModel(this IFileItem item) => new FileItemViewModel(item);

        public static IEnumerable<IFileItemViewModel> ToViewModel(this IEnumerable<IFileItem> items)
            => items.Select(item => ToViewModel(item));

        public static async IAsyncEnumerable<IFileItemViewModel> ToViewModel(this IAsyncEnumerable<IFileItem> items)
        {
            await foreach (var item in items)
            {
                yield return ToViewModel(item);
            }
        }
    }
}
