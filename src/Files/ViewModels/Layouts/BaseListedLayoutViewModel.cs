using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.ViewModels.Layouts
{
    internal abstract class BaseListedLayoutViewModel : BaseLayoutViewModel
    {
        public bool FileNameTeachingTipOpened { get; set; }

        public IEnumerable<ListedItem> SelectedItems { get; protected set; }

        public virtual async Task SelectionChanged(IEnumerable<ListedItem> selectedItems)
        {
            SelectedItems = selectedItems;
            if (SelectedItems.IsAmountInCollection(1))
            {
                await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance, true);
            }
        }
    }
}
