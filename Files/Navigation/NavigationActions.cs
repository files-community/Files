using System;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files
{
    public class NavigationActions
    {
        public async static void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var ContentOwnedViewModelInstance = App.CurrentInstance.ViewModel;
                await ContentOwnedViewModelInstance.RefreshItems();
            });
        }

        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentInstance.NavigationToolbar.CanGoBack = false;
            Frame instanceContentFrame = App.CurrentInstance.ContentFrame;
            if (instanceContentFrame.CanGoBack)
            {
                App.CurrentInstance.ViewModel.CancelLoadAndClearFiles();
                var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;

                SelectSidebarItemFromPath(previousSourcePageType);
                instanceContentFrame.GoBack();
            }
        }

        public static void Forward_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentInstance.NavigationToolbar.CanGoForward = false;
            Frame instanceContentFrame = App.CurrentInstance.ContentFrame;

            if (instanceContentFrame.CanGoForward)
            {
                App.CurrentInstance.ViewModel.CancelLoadAndClearFiles();
                var incomingSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;
                SelectSidebarItemFromPath(incomingSourcePageType);
                App.CurrentInstance.ViewModel.WorkingDirectory = Parameter.ToString();
                instanceContentFrame.GoForward();
            }
        }

        public static void Up_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentInstance.NavigationToolbar.CanNavigateToParent = false;
            Frame instanceContentFrame = App.CurrentInstance.ContentFrame;
            App.CurrentInstance.ViewModel.CancelLoadAndClearFiles();
            var instance = App.CurrentInstance.ViewModel;
            string parentDirectoryOfPath;
            // Check that there isn't a slash at the end
            if ((instance.WorkingDirectory.Count() - 1) - instance.WorkingDirectory.LastIndexOf("\\") > 0)
            {
                parentDirectoryOfPath = instance.WorkingDirectory.Remove(instance.WorkingDirectory.LastIndexOf("\\"));
            }
            else  // Slash found at end
            {
                var currentPathWithoutEndingSlash = instance.WorkingDirectory.Remove(instance.WorkingDirectory.LastIndexOf("\\"));
                parentDirectoryOfPath = currentPathWithoutEndingSlash.Remove(currentPathWithoutEndingSlash.LastIndexOf("\\"));
            }

            SelectSidebarItemFromPath();
            instanceContentFrame.Navigate(App.CurrentInstance.CurrentPageType, parentDirectoryOfPath, new SuppressNavigationTransitionInfo());
        }

        private static void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
        {
            if (incomingSourcePageType == typeof(YourHome) && incomingSourcePageType != null)
            {
                App.CurrentInstance.SidebarSelectedItem = App.sideBarItems.First(x => x.Path.Equals("Home"));
                App.CurrentInstance.NavigationToolbar.PathControlDisplayText = "New tab";
            }
        }
    }
}