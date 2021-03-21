using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.Toolkit.Uwp;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;

namespace Files.Interacts
{
    /// <summary>
    /// This class provides default implementation for BaseLayout commands.
    /// This class can be also inherited from and functions overridden to provide custom functionality
    /// </summary>
    public class BaseLayoutCommandImplementationModel : IBaseLayoutCommandImplementationModel
    {
        #region Singleton

        private NamedPipeAsAppServiceConnection ServiceConnection => associatedInstance?.ServiceConnection;

        private IBaseLayout SlimContentPage => associatedInstance?.SlimContentPage;

        #endregion Singleton

        #region Private Members

        private readonly IShellPage associatedInstance;

        #endregion Private Members

        #region Constructor

        public BaseLayoutCommandImplementationModel(IShellPage associatedInstance)
        {
            this.associatedInstance = associatedInstance;
        }

        #endregion Constructor

        #region IDisposable

        public void Dispose()
        {
            //associatedInstance = null;
        }

        #endregion IDisposable

        #region Command Implementation

        public virtual void RenameItem(RoutedEventArgs e)
        {
            associatedInstance.SlimContentPage.StartRenameItem();
        }

        public virtual async void CreateShortcut(RoutedEventArgs e)
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

        public virtual void SetAsLockscreenBackgroundItem(RoutedEventArgs e)
        {
            associatedInstance.InteractionOperations.SetAsBackground(WallpaperType.LockScreen, SlimContentPage.SelectedItem.ItemPath);
        }

        public virtual void SetAsDesktopBackgroundItem(RoutedEventArgs e)
        {
            associatedInstance.InteractionOperations.SetAsBackground(WallpaperType.Desktop, SlimContentPage.SelectedItem.ItemPath);
        }

        public virtual async void RunAsAdmin(RoutedEventArgs e)
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

        public virtual async void RunAsAnotherUser(RoutedEventArgs e)
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

        public virtual void SidebarPinItem(RoutedEventArgs e)
        {
            SidebarHelpers.PinItems(SlimContentPage.SelectedItems);
        }

        public virtual void SidebarUnpinItem(RoutedEventArgs e)
        {
            SidebarHelpers.UnpinItems(SlimContentPage.SelectedItems);
        }

        public virtual void OpenItem(RoutedEventArgs e)
        {
            associatedInstance.InteractionOperations.OpenSelectedItems(false);
        }

        public virtual void UnpinDirectoryFromSidebar(RoutedEventArgs e)
        {
            App.SidebarPinnedController.Model.RemoveItem(associatedInstance.FilesystemViewModel.WorkingDirectory);
        }

        public virtual void EmptyRecycleBin(RoutedEventArgs e)
        {
            RecycleBinHelpers.EmptyRecycleBin(associatedInstance);
        }

        public virtual void QuickLook(RoutedEventArgs e)
        {
            QuickLookHelpers.ToggleQuickLook(associatedInstance);
        }

        #endregion Command Implementation
    }
}