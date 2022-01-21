using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Files.Backend.ViewModels.ItemListing;
using Files.Backend.ViewModels.Layouts;

namespace Files.Backend.ViewModels.Layouts
{
    public abstract class BaseListedLayoutViewModel : BaseLayoutViewModel
    {
        public bool FileNameTeachingTipOpened { get; set; }


        public List<ListedItemViewModel> SelectedItems { get; protected set; }

        public virtual async Task SelectionChanged(IEnumerable<ListedItemViewModel> selectedItems)
        {
            SelectedItems = selectedItems.ToList();
            if (SelectedItems.IsAmountInCollection(1))
            {
                await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance, true);
            }
        }

        public virtual void SetSelection(IEnumerable<ListedItemViewModel> selectedItems)
        {
            SelectedItems = selectedItems.ToList();
        }
    }
}
