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
            App.OccupiedInstance.Back.IsEnabled = false;
            Frame instanceContentFrame = App.OccupiedInstance.ItemDisplayFrame;
            App.OccupiedInstance.instanceViewModel.CancelLoadAndClearFiles();
            if ((App.OccupiedInstance.ItemDisplayFrame.Content as GenericFileBrowser) != null)
            {
                var instanceContent = (instanceContentFrame.Content as GenericFileBrowser);
                if (instanceContentFrame.CanGoBack)
                {
                    var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.OccupiedInstance.LocationsList.SelectedIndex = 0;
                        App.OccupiedInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.OccupiedInstance;
                        if (Parameter.ToString() == App.DesktopPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == App.DownloadsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == App.DocumentsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == App.PicturesPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == App.MusicPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == App.VideosPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == App.OneDrivePath)
                        {
                            CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.DrivesList.Items)
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
                    instanceContentFrame.GoBack();
                }
            }
            else if ((App.OccupiedInstance.ItemDisplayFrame.Content as PhotoAlbum) != null)
            {
                var instanceContent = (instanceContentFrame.Content as PhotoAlbum);
                if (instanceContentFrame.CanGoBack)
                {
                    var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {

                        App.OccupiedInstance.LocationsList.SelectedIndex = 0;
                        App.OccupiedInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.OccupiedInstance;
                        if (Parameter.ToString() == App.DesktopPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == App.DownloadsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == App.DocumentsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == App.PicturesPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == App.MusicPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == App.VideosPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == App.OneDrivePath)
                        {
                            CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.DrivesList.Items)
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
                    instanceContentFrame.GoBack();
                }
            }
            else if ((App.OccupiedInstance.ItemDisplayFrame.Content as YourHome) != null)
            {
                var instanceContent = (instanceContentFrame.Content as YourHome);

                if (instanceContentFrame.CanGoBack)
                {
                    var previousSourcePageType = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.BackStack[instanceContentFrame.BackStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.OccupiedInstance.LocationsList.SelectedIndex = 0;
                        App.OccupiedInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.OccupiedInstance;
                        if (Parameter.ToString() == App.DesktopPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == App.DownloadsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == App.DocumentsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == App.PicturesPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == App.MusicPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == App.VideosPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == App.OneDrivePath)
                        {
                            CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.DrivesList.Items)
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

                    instanceContentFrame.GoBack();
                }
            }

        }

        public static void Forward_Click(object sender, RoutedEventArgs e)
        {
            App.OccupiedInstance.Forward.IsEnabled = false;
            App.OccupiedInstance.instanceViewModel.CancelLoadAndClearFiles();
            Frame instanceContentFrame = App.OccupiedInstance.ItemDisplayFrame;
            if ((App.OccupiedInstance.ItemDisplayFrame.Content as GenericFileBrowser) != null)
            {
                var instanceContent = (instanceContentFrame.Content as GenericFileBrowser);

                if (instanceContentFrame.CanGoForward)
                {
                    var previousSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.OccupiedInstance.LocationsList.SelectedIndex = 0;
                        App.OccupiedInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.OccupiedInstance;
                        if (Parameter.ToString() == App.DesktopPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == App.DownloadsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == App.DocumentsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == App.PicturesPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == App.MusicPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == App.VideosPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == App.OneDrivePath)
                        {
                            CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.DrivesList.Items)
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

                    instanceContentFrame.GoForward();
                }
            }
            else if ((App.OccupiedInstance.ItemDisplayFrame.Content as PhotoAlbum) != null)
            {
                var instance = App.OccupiedInstance.instanceViewModel;
                var instanceContent = (instanceContentFrame.Content as PhotoAlbum);

                if (instanceContentFrame.CanGoForward)
                {
                    var previousSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.OccupiedInstance.LocationsList.SelectedIndex = 0;
                        App.OccupiedInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.OccupiedInstance;
                        if (Parameter.ToString() == App.DesktopPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == App.DownloadsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == App.DocumentsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == App.PicturesPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == App.MusicPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == App.VideosPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == App.OneDrivePath)
                        {
                            CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.DrivesList.Items)
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

                    instanceContentFrame.GoForward();
                }
            }
            else if ((App.OccupiedInstance.ItemDisplayFrame.Content as YourHome) != null)
            {
                var instanceContent = (instanceContentFrame.Content as YourHome);

                if (instanceContentFrame.CanGoForward)
                {
                    var previousSourcePageType = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].SourcePageType;
                    var Parameter = instanceContentFrame.ForwardStack[instanceContentFrame.ForwardStack.Count - 1].Parameter;

                    if (previousSourcePageType == typeof(YourHome))
                    {
                        App.OccupiedInstance.LocationsList.SelectedIndex = 0;
                        App.OccupiedInstance.PathText.Text = "New tab";
                    }
                    else
                    {
                        var CurrentTabInstance = App.OccupiedInstance;
                        if (Parameter.ToString() == App.DesktopPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 1;
                            CurrentTabInstance.PathText.Text = "Desktop";
                        }
                        else if (Parameter.ToString() == App.DownloadsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 2;
                            CurrentTabInstance.PathText.Text = "Downloads";
                        }
                        else if (Parameter.ToString() == App.DocumentsPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 3;
                            CurrentTabInstance.PathText.Text = "Documents";
                        }
                        else if (Parameter.ToString() == App.PicturesPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 4;
                            CurrentTabInstance.PathText.Text = "Pictures";
                        }
                        else if (Parameter.ToString() == App.MusicPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 5;
                            CurrentTabInstance.PathText.Text = "Music";
                        }
                        else if (Parameter.ToString() == App.VideosPath)
                        {
                            CurrentTabInstance.LocationsList.SelectedIndex = 6;
                            CurrentTabInstance.PathText.Text = "Videos";
                        }
                        else if (Parameter.ToString() == App.OneDrivePath)
                        {
                            CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                            CurrentTabInstance.PathText.Text = "OneDrive";
                        }
                        else
                        {
                            if (Parameter.ToString().Contains("C:\\") || Parameter.ToString().Contains("c:\\"))
                            {
                                CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                            }
                            else
                            {
                                foreach (DriveItem drive in CurrentTabInstance.DrivesList.Items)
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

                    instanceContentFrame.GoForward();
                }
            }
        }

        public static void Up_Click(object sender, RoutedEventArgs e)
        {
            App.OccupiedInstance.Up.IsEnabled = false;
            Frame instanceContentFrame = App.OccupiedInstance.ItemDisplayFrame;
            App.OccupiedInstance.instanceViewModel.CancelLoadAndClearFiles();
            if ((instanceContentFrame.Content as GenericFileBrowser) != null)
            {
                var instance = App.OccupiedInstance.instanceViewModel;
                string parentDirectoryOfPath = null;
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

                var CurrentTabInstance = App.OccupiedInstance;
                if (parentDirectoryOfPath == App.DesktopPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 1;
                    CurrentTabInstance.PathText.Text = "Desktop";
                }
                else if (parentDirectoryOfPath == App.DownloadsPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 2;
                    CurrentTabInstance.PathText.Text = "Downloads";
                }
                else if (parentDirectoryOfPath == App.DocumentsPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 3;
                    CurrentTabInstance.PathText.Text = "Documents";
                }
                else if (parentDirectoryOfPath == App.PicturesPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 4;
                    CurrentTabInstance.PathText.Text = "Pictures";
                }
                else if (parentDirectoryOfPath == App.MusicPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 5;
                    CurrentTabInstance.PathText.Text = "Music";
                }
                else if (parentDirectoryOfPath == App.VideosPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 6;
                    CurrentTabInstance.PathText.Text = "Videos";
                }
                else if (parentDirectoryOfPath == App.OneDrivePath)
                {
                    CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                    CurrentTabInstance.PathText.Text = "OneDrive";
                }
                else
                {
                    if (parentDirectoryOfPath.Contains("C:\\") || parentDirectoryOfPath.Contains("c:\\"))
                    {
                        CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                    }
                    else
                    {
                        foreach (DriveItem drive in CurrentTabInstance.DrivesList.Items)
                        {
                            if (drive.tag.ToString().Contains(parentDirectoryOfPath.Split("\\")[0]))
                            {
                                CurrentTabInstance.DrivesList.SelectedItem = drive;
                                break;
                            }
                        }

                    }
                    CurrentTabInstance.PathText.Text = parentDirectoryOfPath + "\\";
                    instanceContentFrame.Navigate(typeof(GenericFileBrowser), parentDirectoryOfPath + "\\", new SuppressNavigationTransitionInfo());
                    return;
                }
                instanceContentFrame.Navigate(typeof(GenericFileBrowser), parentDirectoryOfPath, new SuppressNavigationTransitionInfo());

            }
            else if ((instanceContentFrame.Content as PhotoAlbum) != null)
            {
                var instance = App.OccupiedInstance.instanceViewModel;
                string parentDirectoryOfPath = null;
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

                var CurrentTabInstance = App.OccupiedInstance;
                if (parentDirectoryOfPath == App.DesktopPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 1;
                    CurrentTabInstance.PathText.Text = "Desktop";
                }
                else if (parentDirectoryOfPath == App.DownloadsPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 2;
                    CurrentTabInstance.PathText.Text = "Downloads";
                }
                else if (parentDirectoryOfPath == App.DocumentsPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 3;
                    CurrentTabInstance.PathText.Text = "Documents";
                }
                else if (parentDirectoryOfPath == App.PicturesPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 4;
                    CurrentTabInstance.PathText.Text = "Pictures";
                }
                else if (parentDirectoryOfPath == App.MusicPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 5;
                    CurrentTabInstance.PathText.Text = "Music";
                }
                else if (parentDirectoryOfPath == App.VideosPath)
                {
                    CurrentTabInstance.LocationsList.SelectedIndex = 6;
                    CurrentTabInstance.PathText.Text = "Videos";
                }
                else if (parentDirectoryOfPath == App.OneDrivePath)
                {
                    CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag.ToString() == "OneDrive").First();
                    CurrentTabInstance.PathText.Text = "OneDrive";
                }
                else
                {
                    if (parentDirectoryOfPath.Contains("C:\\") || parentDirectoryOfPath.Contains("c:\\"))
                    {
                        CurrentTabInstance.DrivesList.SelectedItem = CurrentTabInstance.DrivesList.Items.Where(x => (x as DriveItem).tag == "C:\\").First();
                    }
                    else
                    {
                        foreach (DriveItem drive in CurrentTabInstance.DrivesList.Items)
                        {
                            if (drive.tag.ToString().Contains(parentDirectoryOfPath.Split("\\")[0]))
                            {
                                CurrentTabInstance.DrivesList.SelectedItem = drive;
                                break;
                            }
                        }

                    }
                    CurrentTabInstance.PathText.Text = parentDirectoryOfPath + "\\";
                    instanceContentFrame.Navigate(typeof(PhotoAlbum), parentDirectoryOfPath + "\\", new SuppressNavigationTransitionInfo());
                    return;
                }
                instanceContentFrame.Navigate(typeof(PhotoAlbum), parentDirectoryOfPath, new SuppressNavigationTransitionInfo());
            }
        }
    }
}