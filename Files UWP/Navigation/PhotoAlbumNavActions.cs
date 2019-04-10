using System;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Files.Filesystem;
using Windows.UI.Core;

namespace Files.Navigation
{

    public class PhotoAlbumNavActions
    {
        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.CancelLoadAndClearFiles();

            if (History.HistoryList.Count() > 1)
            {
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                History.AddToForwardList(History.HistoryList[History.HistoryList.Count() - 1]);
                History.HistoryList.RemoveAt(History.HistoryList.Count() - 1);
                App.ViewModel.CancelLoadAndClearFiles();

                // If the item we are navigating back to is a specific library, accomodate this.
                if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                {
                    App.PathText.Text = "Desktop";
                    ProHome.locationsList.SelectedIndex = 1;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                {
                    App.PathText.Text = "Documents";
                    ProHome.locationsList.SelectedIndex = 3;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                {
                    App.PathText.Text = "Downloads";
                    ProHome.locationsList.SelectedIndex = 2;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                {
                    ProHome.locationsList.SelectedIndex = 4;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                    App.PathText.Text = "Pictures";
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                {
                    App.PathText.Text = "Music";
                    ProHome.locationsList.SelectedIndex = 5;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                {
                    App.PathText.Text = "OneDrive";
                    ProHome.drivesList.SelectedIndex = 1;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                {
                    App.PathText.Text = "Videos";
                    ProHome.locationsList.SelectedIndex = 6;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                }
                else
                {

                    if ((History.HistoryList[History.HistoryList.Count - 1]).Contains("C:"))
                    {
                        ProHome.drivesList.SelectedIndex = 0;
                    }
                    App.PathText.Text = (History.HistoryList[History.HistoryList.Count - 1]);
                    App.ViewModel.AddItemsToCollectionAsync(History.HistoryList[History.HistoryList.Count - 1], GenericFileBrowser.GFBPageName); // To take into account the correct index without interference from the folder being navigated to

                }

                if (History.ForwardList.Count == 0)
                {
                    App.ViewModel.FS.isEnabled = false;
                }
                else if (History.ForwardList.Count > 0)
                {
                    App.ViewModel.FS.isEnabled = true;
                }
            }
        }

        public static void Forward_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.CancelLoadAndClearFiles();

            if (History.ForwardList.Count() > 0)
            {
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                App.ViewModel.CancelLoadAndClearFiles();

                if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                {
                    App.PathText.Text = "Desktop";
                    ProHome.locationsList.SelectedIndex = 1;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                {
                    App.PathText.Text = "Documents";
                    ProHome.locationsList.SelectedIndex = 3;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                {
                    App.PathText.Text = "Downloads";
                    ProHome.locationsList.SelectedIndex = 2;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                {
                    ProHome.locationsList.SelectedIndex = 4;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                    App.PathText.Text = "Pictures";
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                {
                    App.PathText.Text = "Music";
                    ProHome.locationsList.SelectedIndex = 5;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                {
                    App.PathText.Text = "OneDrive";
                    ProHome.drivesList.SelectedIndex = 1;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                {
                    App.PathText.Text = "Videos";
                    ProHome.locationsList.SelectedIndex = 6;
                    ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                }
                else
                {
                    Debug.WriteLine("Debug: " + ("Removable Drive (" + History.ForwardList[History.ForwardList.Count() - 1].Split('\\')[0] + "\\)"));
                    if (!History.ForwardList[History.ForwardList.Count() - 1].Split('\\')[0].Contains("C:\\"))
                    {
                        ProHome.drivesList.SelectedIndex = 0;
                    }
                    App.PathText.Text = (History.ForwardList[History.ForwardList.Count() - 1]);
                    App.ViewModel.AddItemsToCollectionAsync(History.ForwardList[History.ForwardList.Count() - 1], GenericFileBrowser.GFBPageName); // To take into account the correct index without interference from the folder being navigated to

                }

                History.ForwardList.RemoveAt(History.ForwardList.Count() - 1);

                if (History.ForwardList.Count == 0)
                {
                    App.ViewModel.FS.isEnabled = false;
                }
                else if (History.ForwardList.Count > 0)
                {
                    App.ViewModel.FS.isEnabled = true;
                }

            }
        }

        public async static void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                App.ViewModel.CancelLoadAndClearFiles();

                App.ViewModel.TextState.isVisible = Visibility.Collapsed;
                App.ViewModel.AddItemsToCollectionAsync(App.PathText.Text, PhotoAlbum.PAPageName);
                if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                {
                    App.PathText.Text = "Desktop";

                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                {
                    App.PathText.Text = "Documents";

                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                {
                    App.PathText.Text = "Downloads";

                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                {

                    App.PathText.Text = "Pictures";
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                {
                    App.PathText.Text = "Music";

                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                {
                    App.PathText.Text = "OneDrive";

                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                {
                    App.PathText.Text = "Videos";

                }
                else
                {
                    App.PathText.Text = (History.HistoryList[History.HistoryList.Count - 1]);

                }
            });
            
        }
    }
}