using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Uwp.Extensions;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using System;

namespace Files.Interacts
{
    public class BaseLayoutCommandImplementationModel : IBaseLayoutCommandImplementationModel
    {
        #region Singleton

        private NamedPipeAsAppServiceConnection ServiceConnection => associatedInstance?.ServiceConnection;

        private IBaseLayout SlimContentPage => associatedInstance?.SlimContentPage;

        #endregion

        #region Private Members

        private readonly IShellPage associatedInstance;

        #endregion

        #region Constructor

        public BaseLayoutCommandImplementationModel(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            //associatedInstance = null;
        }

        #endregion

        #region Command Implementation

        public void RenameItem(RoutedEventArgs e)
        {
            associatedInstance.SlimContentPage.StartRenameItem();
        }

        public async void CreateShortcut(RoutedEventArgs e)
        {
            foreach (ListedItem selectedItem in SlimContentPage.SelectedItems)
            {
                if (ServiceConnection != null)
                {
                    var value = new ValueSet()
                    {
                        { "Arguments", "FileOperation" },
                        { "fileop", "CreateLink" },
                        { "targetpath", selectedItem.ItemPath },
                        { "arguments", "" },
                        { "workingdir", "" },
                        { "runasadmin", false },
                        {
                            "filepath",
                            System.IO.Path.Combine(associatedInstance.FilesystemViewModel.WorkingDirectory,
                                string.Format("ShortcutCreateNewSuffix".GetLocalized(), selectedItem.ItemName) + ".lnk")
                        }
                    };
                    await ServiceConnection.SendMessageAsync(value);
                }
            }
        }

        public void SetAsLockscreenBackgroundItem(RoutedEventArgs e)
        {
            associatedInstance.InteractionOperations.SetAsBackground(WallpaperType.LockScreen, SlimContentPage.SelectedItem.ItemPath);
        }

        public void SetAsDesktopBackgroundItem(RoutedEventArgs e)
        {
            associatedInstance.InteractionOperations.SetAsBackground(WallpaperType.Desktop, SlimContentPage.SelectedItem.ItemPath);
        }

        public async void RunAsAdmin(RoutedEventArgs e)
        {
            if (ServiceConnection != null)
            {
                await ServiceConnection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "InvokeVerb" },
                    { "FilePath", SlimContentPage.SelectedItem.ItemPath },
                    { "Verb", "runas" }
                });
            }
        }

        public async void RunAsAnotherUser(RoutedEventArgs e)
        {
            if (ServiceConnection != null)
            {
                await ServiceConnection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "InvokeVerb" },
                    { "FilePath", SlimContentPage.SelectedItem.ItemPath },
                    { "Verb", "runasuser" }
                });
            }
        }

        public void SidebarPinItem(RoutedEventArgs e)
        {
            SidebarHelpers.PinItems(SlimContentPage.SelectedItems);
        }

        public void SidebarUnpinItem(RoutedEventArgs e)
        {
            SidebarHelpers.UnpinItems(SlimContentPage.SelectedItems);
        }

        public void OpenItem(RoutedEventArgs e)
        {
            associatedInstance.InteractionOperations.OpenSelectedItems(false);
        }

        public void UnpinDirectoryFromSidebar(RoutedEventArgs e)
        {
            App.SidebarPinnedController.Model.RemoveItem(associatedInstance.FilesystemViewModel.WorkingDirectory);
        }

        public void EmptyRecycleBin(RoutedEventArgs e)
        {
            RecycleBinHelpers.EmptyRecycleBin(associatedInstance);
        }

        public void QuickLook(RoutedEventArgs e)
        {
            QuickLookHelpers.ToggleQuickLook(associatedInstance);
        }

        #endregion
    }
}
