using Microsoft.Toolkit.Uwp;
using System;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.UI.Core;

namespace Files.Helpers
{
    public static class QuickLookHelpers
    {
        public static async void ToggleQuickLook(IShellPage associatedInstance)
        {
            try
            {
                if (associatedInstance.SlimContentPage.IsItemSelected && !associatedInstance.SlimContentPage.IsRenamingItem)
                {
                    Debug.WriteLine("Toggle QuickLook");
                    if (associatedInstance.ServiceConnection != null)
                    {
                        await associatedInstance.ServiceConnection.SendMessageAsync(new ValueSet()
                        {
                            { "path", associatedInstance.SlimContentPage.SelectedItem.ItemPath },
                            { "Arguments", "ToggleQuickLook" }
                        });
                    }
                }
            }
            catch (FileNotFoundException)
            {
                await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalized(), "FileNotFoundPreviewDialog/Text".GetLocalized());
                associatedInstance.NavigationToolbar.CanRefresh = false;
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var ContentOwnedViewModelInstance = associatedInstance.FilesystemViewModel;
                    ContentOwnedViewModelInstance?.RefreshItems(null);
                });
            }
        }
    }
}