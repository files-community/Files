using Files.Extensions;
using Files.Filesystem;
using Files.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Backend.ViewModels.ItemListing;
using Files.Backend.ViewModels.Layouts;

namespace Files.Backend.ViewModels.Layouts
{
    public abstract class BaseListedLayoutViewModel : BaseLayoutViewModel
    {
        public bool FileNameTeachingTipOpened { get; set; }

        public ListedItemViewModel SelectedItem { get; private set; }

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

        /// <summary>
        /// Executes the item tap action.
        /// </summary>
        /// <param name="listedItem"></param>
        /// <param name="ctrlPressed"></param>
        /// <param name="shiftPressed"></param>
        /// <returns>True, if operation completed successfully, otherwise false</returns>
        public virtual bool ItemTapped(ListedItemViewModel listedItem, bool ctrlPressed, bool shiftPressed)
        {
            // Skip code if the control or shift key is pressed or if the user is using multi-select
            if (ctrlPressed || shiftPressed || MainViewModel.MultiselectEnabled)
            {
                return true;
            }

            // Check if the setting to open items with a single click is turned on
            if ((UserSettingsService.PreferencesSettingsService.OpenFoldersWithOneClick && listedItem is ListedFolderViewModel)
                || (UserSettingsService.PreferencesSettingsService.OpenFilesWithOneClick && listedItem is ListedFileViewModel))
            {
                ResetRenameDoubleClick();
                NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);

                return true;
            }

            return false;
        }

        public virtual void ItemDoubleTapped(ListedItemViewModel listedItem)
        {
            if ((!UserSettingsService.PreferencesSettingsService.OpenFilesWithOneClick && listedItem is ListedFileViewModel)
                 || (!UserSettingsService.PreferencesSettingsService.OpenFoldersWithOneClick && listedItem is ListedFolderViewModel))
            {
                NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
            }

            ResetRenameDoubleClick();
        }

        public virtual async Task<bool> PreviewKeyDown(VirtualKey key, bool menuKeyDown, bool ctrlPressed, bool shiftPressed, bool isHeaderFocused, bool isFooterFocused)
        {
            if (key == VirtualKey.Enter && !menuKeyDown)
            {
                if (!IsRenamingItem && !isHeaderFocused && !isFooterFocused)
                {
                    NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
                    return true;
                }
            }
            else if (key == VirtualKey.Enter && menuKeyDown)
            {
                FilePropertiesHelpers.ShowProperties(ParentShellPageInstance);
                return true;
            }
            else if (key == VirtualKey.Space)
            {
                if (!IsRenamingItem && !isHeaderFocused && !isFooterFocused && !ParentShellPageInstance.NavToolbarViewModel.IsEditModeEnabled)
                {
                    await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance);
                    return true;
                }
            }
            else if (menuKeyDown && (key == VirtualKey.Left || key == VirtualKey.Right || key == VirtualKey.Up))
            {
                // Unfocus the GridView so keyboard shortcut can be handled
                NavToolbar?.Focus(FocusState.Pointer);
            }
            else if (menuKeyDown && shiftPressed && key == VirtualKey.Add)
            {
                // Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
                NavToolbar?.Focus(FocusState.Pointer);
            }

            return false;
        }
    }
}
