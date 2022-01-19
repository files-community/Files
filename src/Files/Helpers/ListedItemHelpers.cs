using Files.Filesystem;
using Files.ViewModels;

namespace Files.Helpers
{
    public static class ListedItemHelpers
    {
        /// <summary>
        /// Check if the <see cref="ListedItem"/> is a image file.
        /// </summary>
        /// <param name="listedItem"></param>
        /// <returns><c>true</c> if the <see cref="ListedItem"/> is an image, otherwise <c>false</c></returns>
        public static bool IsImage(this ListedItem listedItem)
        {
            var selectedItemsViewModel = new SelectedItemsPropertiesViewModel { SelectedItemsCount = 1 };
            selectedItemsViewModel.CheckFileExtension(listedItem.FileExtension);

            return selectedItemsViewModel.IsSelectedItemImage;
        }
    }
}
