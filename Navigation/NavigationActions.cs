using Files;
using Files.Filesystem;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Files.Navigation
{
    public class NavigationActions
    {
        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: THIS CHECK WAS REMOVED, REVERT BACK IF THINGS BREAK
            //if (ItemViewModel.IsTerminated == false)
            //{
            ItemViewModel.tokenSource.Cancel();
            ItemViewModel.FilesAndFolders.Clear();
            //}

            if (History.HistoryList.Count > 1)
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                //Debug.WriteLine("\nBefore Removals");
                //ArrayDiag.DumpArray();
                History.AddToForwardList(History.HistoryList[History.HistoryList.Count - 1]);
                History.HistoryList.RemoveAt(History.HistoryList.Count - 1);
                //Debug.WriteLine("\nAfter Removals");
                //ArrayDiag.DumpArray();

                ItemViewModel.FilesAndFolders.Clear();

                // If the item we are navigating back to is a specific library, accomodate this.
                if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                {
                    GenericFileBrowser.P.path = "Desktop";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DesktopIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Desktop";
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                {
                    GenericFileBrowser.P.path = "Documents";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DocumentsIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Documents";
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                {
                    GenericFileBrowser.P.path = "Downloads";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DownloadsIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Downloads";
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                {
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "PicturesIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Pictures";
                    GenericFileBrowser.P.path = "Pictures";
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                {
                    GenericFileBrowser.P.path = "Music";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "MusicIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Music";
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                {
                    GenericFileBrowser.P.path = "OneDrive";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "OneD_IC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search OneDrive";
                }
                else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                {
                    GenericFileBrowser.P.path = "Videos";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "VideosIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Videos";
                }
                else
                {

                    if ((History.HistoryList[History.HistoryList.Count - 1]).Contains("C:"))
                    {
                        foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                        {
                            if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "LocD_IC")
                            {
                                MainPage.Select.itemSelected = NavItemChoice;
                                break;
                            }
                        }
                    }
                    GenericFileBrowser.P.path = (History.HistoryList[History.HistoryList.Count - 1]);
                    ItemViewModel.ViewModel = new ItemViewModel(History.HistoryList[History.HistoryList.Count - 1], GenericFileBrowser.GFBPageName); // To take into account the correct index without interference from the folder being navigated to

                }

                if (History.ForwardList.Count == 0)
                {
                    ItemViewModel.FS.isEnabled = false;
                }
                else if (History.ForwardList.Count > 0)
                {
                    ItemViewModel.FS.isEnabled = true;
                }


            }

        }

        public static void Forward_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: THIS CHECK WAS REMOVED, REVERT BACK IF THINGS BREAK
            //if (ItemViewModel.IsTerminated == false)
            //{
            ItemViewModel.tokenSource.Cancel();
            ItemViewModel.FilesAndFolders.Clear();
            //}

            if (History.ForwardList.Count() > 0)
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ItemViewModel.FilesAndFolders.Clear();


                if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                {
                    GenericFileBrowser.P.path = "Desktop";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DesktopIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DesktopPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Desktop";
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                {
                    GenericFileBrowser.P.path = "Documents";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DocumentsIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DocumentsPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Documents";
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
                {
                    GenericFileBrowser.P.path = "Downloads";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "DownloadsIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.DownloadsPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Downloads";
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
                {
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "PicturesIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(PhotoAlbum), YourHome.PicturesPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Pictures";
                    GenericFileBrowser.P.path = "Pictures";
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
                {
                    GenericFileBrowser.P.path = "Music";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "MusicIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.MusicPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Music";
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
                {
                    GenericFileBrowser.P.path = "OneDrive";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "OneD_IC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.OneDrivePath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search OneDrive";
                }
                else if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
                {
                    GenericFileBrowser.P.path = "Videos";
                    foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Name.ToString() == "VideosIC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), YourHome.VideosPath, new SuppressNavigationTransitionInfo());
                    MainPage.accessibleAutoSuggestBox.PlaceholderText = "Search Videos";
                }
                else
                {
                    Debug.WriteLine("Debug: " + ("Removable Drive (" + History.ForwardList[History.ForwardList.Count() - 1].Split('\\')[0] + "\\)"));
                    if (!History.ForwardList[History.ForwardList.Count() - 1].Split('\\')[0].Contains("C:\\"))
                    {

                        foreach (Microsoft.UI.Xaml.Controls.NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                        {
                            if (NavItemChoice is Microsoft.UI.Xaml.Controls.NavigationViewItem && NavItemChoice.Content.ToString() == ("Removable Drive (" + History.ForwardList[History.ForwardList.Count() - 1].Split('\\')[0] + "\\)"))
                            {

                                MainPage.Select.itemSelected = NavItemChoice;
                                break;
                            }
                        }
                    }
                    GenericFileBrowser.P.path = (History.ForwardList[History.ForwardList.Count() - 1]);
                    ItemViewModel.ViewModel = new ItemViewModel(History.ForwardList[History.ForwardList.Count() - 1], GenericFileBrowser.GFBPageName); // To take into account the correct index without interference from the folder being navigated to

                }



                History.ForwardList.RemoveAt(History.ForwardList.Count() - 1);
                //ArrayDiag.DumpForwardArray();

                if (History.ForwardList.Count == 0)
                {
                    ItemViewModel.FS.isEnabled = false;
                }
                else if (History.ForwardList.Count > 0)
                {
                    ItemViewModel.FS.isEnabled = true;
                }

            }
        }

        public static void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ItemViewModel.tokenSource.Cancel();
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            ItemViewModel.FilesAndFolders.Clear();
            ItemViewModel.ViewModel = new ItemViewModel(ItemViewModel.PUIP.Path, GenericFileBrowser.GFBPageName);
            if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
            {
                GenericFileBrowser.P.path = "Desktop";

            }
            else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
            {
                GenericFileBrowser.P.path = "Documents";

            }
            else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
                GenericFileBrowser.P.path = "Downloads";

            }
            else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))
            {

                GenericFileBrowser.P.path = "Pictures";
            }
            else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))
            {
                GenericFileBrowser.P.path = "Music";

            }
            else if ((History.HistoryList[History.HistoryList.Count - 1]) == (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
                GenericFileBrowser.P.path = "OneDrive";

            }
            else if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))
            {
                GenericFileBrowser.P.path = "Videos";

            }
            else
            {
                GenericFileBrowser.P.path = (History.HistoryList[History.HistoryList.Count - 1]);

            }

        }
    }
}