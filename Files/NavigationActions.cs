using System;
using Files.Filesystem;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Files.View_Models;
using Files.Controls;

namespace Files
{
    public class NavigationActions
    {
        public async static void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var ContentOwnedViewModelInstance = App.CurrentInstance.ViewModel;
                ContentOwnedViewModelInstance.AddItemsToCollectionAsync(ContentOwnedViewModelInstance.Universal.path);
            });
        }

        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentInstance.CanGoBack = false;
            Frame instanceContentFrame = App.CurrentInstance.ContentFrame;
            if (instanceContentFrame.CanGoBack)
            {
                App.CurrentInstance.ViewModel.CancelLoadAndClearFiles();
                var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                SelectSidebarItemFromPath(Parameter.ToString(), previousSourcePageType);
                instanceContentFrame.GoBack();
            }
        }

        public static void Forward_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentInstance.CanGoForward = false;
            Frame instanceContentFrame = App.CurrentInstance.ContentFrame;

            if (instanceContentFrame.CanGoForward)
            {
                App.CurrentInstance.ViewModel.CancelLoadAndClearFiles();
                var incomingSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;
                SelectSidebarItemFromPath(Parameter.ToString(), incomingSourcePageType);
                App.CurrentInstance.ViewModel.Universal.path = Parameter.ToString();
                instanceContentFrame.GoForward();
            }
        }

        public static void Up_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentInstance.CanNavigateToParent = false;
            Frame instanceContentFrame = App.CurrentInstance.ContentFrame;
            App.CurrentInstance.ViewModel.CancelLoadAndClearFiles();
            var instance = App.CurrentInstance.ViewModel;
            string parentDirectoryOfPath;
            // Check that there isn't a slash at the end
            if ((instance.Universal.path.Count() - 1) - instance.Universal.path.LastIndexOf("\\") > 0)
            {
                parentDirectoryOfPath = instance.Universal.path.Remove(instance.Universal.path.LastIndexOf("\\"));
            }
            else  // Slash found at end
            {
                var currentPathWithoutEndingSlash = instance.Universal.path.Remove(instance.Universal.path.LastIndexOf("\\"));
                parentDirectoryOfPath = currentPathWithoutEndingSlash.Remove(currentPathWithoutEndingSlash.LastIndexOf("\\"));
            }

            SelectSidebarItemFromPath(parentDirectoryOfPath, null);
            instanceContentFrame.Navigate(App.CurrentInstance.CurrentPageType, parentDirectoryOfPath, new SuppressNavigationTransitionInfo());
        }

        private static void SelectSidebarItemFromPath(string Parameter, Type incomingSourcePageType)
        {
            if (incomingSourcePageType == typeof(YourHome) && incomingSourcePageType != null)
            {
                (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => (x as INavigationControlItem) == App.sideBarItems[0]);
                App.CurrentInstance.PathControlDisplayText = "New tab";
            }
            else
            {
                var CurrentTabInstance = App.CurrentInstance;
                if (Parameter.ToString() == App.AppSettings.DesktopPath)
                {
                    (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => (x as INavigationControlItem).Path.Equals(App.AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathControlDisplayText = "Desktop";
                }
                else if (Parameter.ToString() == App.AppSettings.DownloadsPath)
                {
                    (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => (x as INavigationControlItem).Path.Equals(App.AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathControlDisplayText = "Downloads";
                }
                else if (Parameter.ToString() == App.AppSettings.DocumentsPath)
                {
                    (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => (x as INavigationControlItem).Path.Equals(App.AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathControlDisplayText = "Documents";
                }
                else if (Parameter.ToString() == App.AppSettings.PicturesPath)
                {
                    (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => (x as INavigationControlItem).Path.Equals(App.AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathControlDisplayText = "Pictures";
                }
                else if (Parameter.ToString() == App.AppSettings.MusicPath)
                {
                    (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => (x as INavigationControlItem).Path.Equals(App.AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathControlDisplayText = "Music";
                }
                else if (Parameter.ToString() == App.AppSettings.VideosPath)
                {
                    (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = App.sideBarItems.First(x => (x as INavigationControlItem).Path.Equals(App.AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathControlDisplayText = "Videos";
                }
                else if (Parameter.ToString() == App.AppSettings.OneDrivePath)
                {
                    (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = SettingsViewModel.foundDrives.Where(x => (x as DriveItem).tag == "OneDrive").First();
                    CurrentTabInstance.PathControlDisplayText = "OneDrive";
                }
                else
                {
                    if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                    {
                        (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = SettingsViewModel.foundDrives.Where(x => (x as DriveItem).tag == "C:\\").First();
                    }
                    else
                    {
                        foreach (DriveItem drive in SettingsViewModel.foundDrives)
                        {
                            if (drive.tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                            {
                                (App.CurrentInstance as ProHome).SidebarControl.SidebarNavView.SelectedItem = drive;
                                break;
                            }
                        }

                    }
                    CurrentTabInstance.PathControlDisplayText = Parameter.ToString();
                }
            }
        }
    }
}