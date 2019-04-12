using System;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Files.Navigation
{
    public class NavigationActions
    {
        public async static void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                App.ViewModel.CancelLoadAndClearFiles();

                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                App.ViewModel.AddItemsToCollectionAsync(App.ViewModel.Universal.path, GenericFileBrowser.GFBPageName);
                
            });
        }
    }
}