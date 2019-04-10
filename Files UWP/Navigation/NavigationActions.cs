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
        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.CancelLoadAndClearFiles();

            //if (History.HistoryList.Count > 1)
            //{
                
                App.ViewModel.TextState.isVisible = Visibility.Collapsed;

                //History.AddToForwardList(History.HistoryList[History.HistoryList.Count - 1]);
                //History.HistoryList.RemoveAt(History.HistoryList.Count - 1);

                //App.ViewModel.CancelLoadAndClearFiles();
                if (ProHome.accessibleContentFrame.CanGoBack)
                {

                    //// If the item we are navigating back to is a specific library, accomodate this.
                    //PageStackEntry pse = ProHome.accessibleContentFrame.BackStack[0];
                    //    try
                    //    {
                    //        Debug.WriteLine(pse.Parameter.ToString());
                    //    }
                    //    catch
                    //    {

                    //    }
                    //if (ProHome.accessibleContentFrame.BackStack[(ProHome.accessibleContentFrame.BackStackDepth - 1)].Parameter.ToString() == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                    //{
                    //    App.PathText.Text = "Desktop";
                    //    ProHome.locationsList.SelectedIndex = 1;
                    //    //ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());
                    //}
                    //else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                    //{
                    //    App.PathText.Text = "Documents";
                    //    ProHome.locationsList.SelectedIndex = 3;
                    //    //ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                    //}
                    //else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                    //{
                    //    App.PathText.Text = "Downloads";
                    //    ProHome.locationsList.SelectedIndex = 2;
                    //    //ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                    //}
                    //else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                    //{
                    //    ProHome.locationsList.SelectedIndex = 4;
                    //    //ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                    //    App.PathText.Text = "Pictures";
                    //}
                    //else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                    //{
                    //    App.PathText.Text = "Music";
                    //    ProHome.locationsList.SelectedIndex = 5;
                    //    //ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                    //}
                    //else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                    //{
                    //    App.PathText.Text = "OneDrive";
                    //    ProHome.drivesList.SelectedIndex = 1;
                    //    //ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                    //}
                    //else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                    //{
                    //    App.PathText.Text = "Videos";
                    //    ProHome.locationsList.SelectedIndex = 6;
                    //    //ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                    //}
                    //else if (ProHome.accessibleContentFrame.BackStack[(ProHome.accessibleContentFrame.BackStackDepth - 1)].Parameter == null)
                    //{
                    //    if (ProHome.accessibleContentFrame.BackStack[(ProHome.accessibleContentFrame.BackStackDepth - 1)].SourcePageType == typeof(YourHome))
                    //    {
                    //        App.PathText.Text = "This PC";
                    //    }

                    //}
                    //else
                    //{

                    //    if ((History.HistoryList[History.HistoryList.Count - 1]).Contains("C:"))
                    //    {
                    //        ProHome.drivesList.SelectedIndex = 0;
                    //    }
                    //    App.PathText.Text = (History.HistoryList[History.HistoryList.Count - 1]);
                    //    //App.ViewModel.AddItemsToCollectionAsync(History.HistoryList[History.HistoryList.Count - 1], GenericFileBrowser.GFBPageName); // To take into account the correct index without interference from the folder being navigated to

                    //}

                    ProHome.accessibleContentFrame.GoBack();
                }

                //if (History.ForwardList.Count == 0)
                //{
                //    App.ViewModel.FS.isEnabled = false;
                //}
                //else if (History.ForwardList.Count > 0)
                //{
                //    App.ViewModel.FS.isEnabled = true;
                //}

            //}

        }

        public static void Forward_Click(object sender, RoutedEventArgs e)
        {

            App.ViewModel.CancelLoadAndClearFiles();

            //if (History.ForwardList.Count() > 0)
            //{
            //    App.ViewModel.TextState.isVisible = Visibility.Collapsed;
            //    App.ViewModel.CancelLoadAndClearFiles();

            //    if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
            //    {
            //        App.PathText.Text = "Desktop";
            //        ProHome.locationsList.SelectedIndex = 1;
            //        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());
            //    }
            //    else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
            //    {
            //        App.PathText.Text = "Documents";
            //        ProHome.locationsList.SelectedIndex = 3;
            //        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
            //    }
            //    else if ((History.ForwardList[History.ForwardList.Count() - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            //    {
            //        App.PathText.Text = "Downloads";
            //        ProHome.locationsList.SelectedIndex = 2;
            //        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
            //    }
            //    else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
            //    {
            //        ProHome.locationsList.SelectedIndex = 4;
            //        ProHome.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
            //        App.PathText.Text = "Pictures";
            //    }
            //    else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
            //    {
            //        App.PathText.Text = "Music";
            //        ProHome.locationsList.SelectedIndex = 5;
            //        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
            //    }
            //    else if ((History.ForwardList[History.ForwardList.Count() - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            //    {
            //        App.PathText.Text = "OneDrive";
            //        ProHome.drivesList.SelectedIndex = 1;
            //        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
            //    }
            //    else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
            //    {
            //        App.PathText.Text = "Videos";
            //        ProHome.locationsList.SelectedIndex = 6;
            //        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
            //    }
            //    else
            //    {
            //        //Debug.WriteLine("Debug: " + ("Removable Drive (" + History.ForwardList[History.ForwardList.Count() - 1].Split('\\')[0] + "\\)"));
            //        if (!History.ForwardList[History.ForwardList.Count - 1].Split('\\')[0].Contains("C:\\"))
            //        {
            //            ProHome.drivesList.SelectedIndex = 0;
            //        }
            //        App.PathText.Text = (History.ForwardList[History.ForwardList.Count - 1]);
            //        ProHome.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), History.ForwardList[History.ForwardList.Count - 1], new SuppressNavigationTransitionInfo());
            //    }



            //    History.ForwardList.RemoveAt(History.ForwardList.Count() - 1);

            //    if (History.ForwardList.Count == 0)
            //    {
            //        App.ViewModel.FS.isEnabled = false;
            //    }
            //    else if (History.ForwardList.Count > 0)
            //    {
            //        App.ViewModel.FS.isEnabled = true;
            //    }

            //}
        }

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