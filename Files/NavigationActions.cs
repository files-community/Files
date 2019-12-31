using System;
using Files.Filesystem;
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
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                var ContentOwnedViewModelInstance = App.OccupiedInstance.instanceViewModel;
                ContentOwnedViewModelInstance.AddItemsToCollectionAsync(ContentOwnedViewModelInstance.Universal.path);
            });
        }

        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            App.OccupiedInstance.RibbonArea.Back.IsEnabled = false;
            Frame instanceContentFrame = App.OccupiedInstance.ItemDisplayFrame;
            if (instanceContentFrame.CanGoBack)
            {
                App.OccupiedInstance.instanceViewModel.CancelLoadAndClearFiles();
                var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                SelectSidebarItemFromPath(Parameter.ToString(), previousSourcePageType);
                instanceContentFrame.GoBack();
            }
        }

        public static void Forward_Click(object sender, RoutedEventArgs e)
        {
            App.OccupiedInstance.RibbonArea.Forward.IsEnabled = false;
            Frame instanceContentFrame = App.OccupiedInstance.ItemDisplayFrame;

            if (instanceContentFrame.CanGoForward)
            {
                App.OccupiedInstance.instanceViewModel.CancelLoadAndClearFiles();
                var incomingSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;
                SelectSidebarItemFromPath(Parameter.ToString(), incomingSourcePageType);
                App.OccupiedInstance.instanceViewModel.Universal.path = Parameter.ToString();
                instanceContentFrame.GoForward();
            }
        }

        public static void Up_Click(object sender, RoutedEventArgs e)
        {
            App.OccupiedInstance.RibbonArea.Up.IsEnabled = false;
            Frame instanceContentFrame = App.OccupiedInstance.ItemDisplayFrame;
            App.OccupiedInstance.instanceViewModel.CancelLoadAndClearFiles();
            var instance = App.OccupiedInstance.instanceViewModel;
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
            instanceContentFrame.Navigate(App.OccupiedInstance.ItemDisplayFrame.CurrentSourcePageType, parentDirectoryOfPath, new SuppressNavigationTransitionInfo());
        }

        private static void SelectSidebarItemFromPath(string Parameter, Type incomingSourcePageType)
        {
            if (incomingSourcePageType == typeof(YourHome) && incomingSourcePageType != null)
            {
                App.OccupiedInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem) == App.sideBarItems[0]);
                App.OccupiedInstance.PathText.Text = "New tab";
            }
            else
            {
                var CurrentTabInstance = App.OccupiedInstance;
                if (Parameter.ToString() == App.DesktopPath)
                {
                    CurrentTabInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DesktopPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathText.Text = "Desktop";
                }
                else if (Parameter.ToString() == App.DownloadsPath)
                {
                    CurrentTabInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DownloadsPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathText.Text = "Downloads";
                }
                else if (Parameter.ToString() == App.DocumentsPath)
                {
                    CurrentTabInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.DocumentsPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathText.Text = "Documents";
                }
                else if (Parameter.ToString() == App.PicturesPath)
                {
                    CurrentTabInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.PicturesPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathText.Text = "Pictures";
                }
                else if (Parameter.ToString() == App.MusicPath)
                {
                    CurrentTabInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.MusicPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathText.Text = "Music";
                }
                else if (Parameter.ToString() == App.VideosPath)
                {
                    CurrentTabInstance.LocationsList.SelectedItem = App.sideBarItems.First(x => (x as SidebarItem).Path.Equals(App.VideosPath, StringComparison.OrdinalIgnoreCase));
                    CurrentTabInstance.PathText.Text = "Videos";
                }
                else if (Parameter.ToString() == App.OneDrivePath)
                {
                    CurrentTabInstance.DrivesList.SelectedItem = App.foundDrives.Where(x => (x as DriveItem).tag == "OneDrive").First();
                    CurrentTabInstance.PathText.Text = "OneDrive";
                }
                else
                {
                    if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                    {
                        CurrentTabInstance.DrivesList.SelectedItem = App.foundDrives.Where(x => (x as DriveItem).tag == "C:\\").First();
                    }
                    else
                    {
                        foreach (DriveItem drive in App.foundDrives)
                        {
                            if (drive.tag.ToString().Contains(Parameter.ToString().Split("\\")[0]))
                            {
                                CurrentTabInstance.DrivesList.SelectedItem = drive;
                                break;
                            }
                        }

                    }
                    CurrentTabInstance.PathText.Text = Parameter.ToString();
                }
            }
        }
    }
}