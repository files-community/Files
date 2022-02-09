using Files.Backend.ViewModels.Layouts.ItemListingModels;

namespace Files.Layouts.Listing
{
    internal sealed class LayoutItemsListing<TItemListingModel>
        where TItemListingModel : ItemListingModel
    {
        public TItemListingModel ItemListingModel { get; }

        public LayoutItemsListing(TItemListingModel itemListingModel)
        {
            this.ItemListingModel = itemListingModel;
        }
    }
}
