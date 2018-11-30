//  ---- NavigationSystem.cs ----
//
//   Copyright 2018 Luke Blevins
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//  ---- This file contains code for the back and forward navigation ---- 
//


using Files;
using ItemListPresenter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Navigation
{
    public class History
    {
        public static List<string> HistoryList = new List<string>();                // The list of paths previously navigated to
        public static void AddToHistory(string PathToBeAdded)
        {
            if (HistoryList.Count < 25)                                              // If HistoryList is currently less than 25 items and 
            {
                if (HistoryList.Count > 0)                                           // If there are items in HistoryList
                {
                    if (HistoryList[HistoryList.Count - 1] != PathToBeAdded)         // Make sure the item being added is not already added
                    {
                        HistoryList.Add(PathToBeAdded);
                    }
                }
                else                                                                // If there are no items in HistoryList
                {
                    HistoryList.Add(PathToBeAdded);
                }

            }
            else if ((HistoryList.Count >= 25) && (HistoryList[HistoryList.Count - 1] != PathToBeAdded))     // If History list is exactly 25 items (or greater) and the item being added is not already added
            {
                for (int i = 0; i < (HistoryList.Count - 1); i++)
                {
                    HistoryList[i] = HistoryList[i + 1];                // Shift list contents left by one to delete first item, effectively making space for next item 
                }
                HistoryList[24] = PathToBeAdded;                        // Add new item in freed spot
            }
        }

        public static List<string> ForwardList = new List<string>();
        public static void AddToForwardList(string PathToBeAdded)
        {
            if (ForwardList.Count > 0)
            {
                if (ForwardList[ForwardList.Count - 1] != PathToBeAdded)
                {
                    ForwardList.Add(PathToBeAdded);
                }
            }
            else
            {
                ForwardList.Add(PathToBeAdded);
            }

        }
    }

    public class ArrayDiag
    {

        public static void DumpArray()
        {
            foreach (string s in History.HistoryList)
            {
                Debug.Write(s + ", ");
            }
            Debug.WriteLine(" ");
        }

        public static void DumpForwardArray()
        {
            foreach (string s in History.ForwardList)
            {
                Debug.Write(s + ", ");
            }
            Debug.WriteLine(" ");
        }
    }

    public class BackState : INotifyPropertyChanged
    {


        public bool _isEnabled;
        public bool isEnabled
        {
            get
            {
                return _isEnabled;
            }

            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    NotifyPropertyChanged("isEnabled");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }

    public class ForwardState : INotifyPropertyChanged
    {


        public bool _isEnabled;
        public bool isEnabled
        {
            get
            {
                return _isEnabled;
            }

            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    NotifyPropertyChanged("isEnabled");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }

    public class NavigationActions
    {
        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            if (ItemViewModel.IsTerminated == false)
            {
                ItemViewModel.IsStopRequested = true;
            }

            if (History.HistoryList.Count() > 1)
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                //Debug.WriteLine("\nBefore Removals");
                //ArrayDiag.DumpArray();
                History.AddToForwardList(History.HistoryList[History.HistoryList.Count - 1]);
                History.HistoryList.RemoveAt(History.HistoryList.Count - 1);
                //Debug.WriteLine("\nAfter Removals");
                //ArrayDiag.DumpArray();

                ItemViewModel.FilesAndFolders.Clear();


                if ((History.HistoryList[History.HistoryList.Count - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                {
                    GenericFileBrowser.P.path = "Desktop";
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "DesktopIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "DocumentsIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "DownloadsIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "PicturesIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "MusicIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "OneD_IC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "VideosIC")
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
                    GenericFileBrowser.P.path = (History.HistoryList[History.HistoryList.Count - 1]);
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "LocD_IC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    ItemViewModel.ViewModel = new ItemViewModel(History.HistoryList[History.HistoryList.Count - 1]); // To take into account the correct index without interference from the folder being navigated to
                }
                GenericFileBrowser.UpdateAllBindings();

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
            if(ItemViewModel.IsTerminated == false)
            {
                ItemViewModel.IsStopRequested = true;
            }

            if (History.ForwardList.Count() > 0)
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ItemViewModel.FilesAndFolders.Clear();

                
                if ((History.ForwardList[History.ForwardList.Count() - 1]) == Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory))
                {
                    GenericFileBrowser.P.path = "Desktop";
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "DesktopIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "DocumentsIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "DownloadsIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "PicturesIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "MusicIC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "OneD_IC")
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
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "VideosIC")
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
                    GenericFileBrowser.P.path = (History.ForwardList[History.ForwardList.Count() - 1]);
                    foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
                    {
                        if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "LocD_IC")
                        {
                            MainPage.Select.itemSelected = NavItemChoice;
                            break;
                        }
                    }
                    ItemViewModel.ViewModel = new ItemViewModel(History.ForwardList[History.ForwardList.Count() - 1]); // To take into account the correct index without interference from the folder being navigated to
                }


                
                History.ForwardList.RemoveAt(History.ForwardList.Count() - 1);
                GenericFileBrowser.UpdateAllBindings();
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
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            ItemViewModel.FilesAndFolders.Clear();
            ItemViewModel.ViewModel = new ItemViewModel(ItemViewModel.PUIP.Path);
            //GenericFileBrowser.P.path = ItemViewModel.PUIP.Path;
            GenericFileBrowser.UpdateAllBindings();
        }
    }

    public class PhotoAlbumNavActions
    {
        public static void Back_Click(object sender, RoutedEventArgs e)
        {
            if (ItemViewModel.IsTerminated == false)
            {
                ItemViewModel.IsStopRequested = true;
            }

            if (History.HistoryList.Count() > 1)
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                Debug.WriteLine("\nBefore Removals");
                ArrayDiag.DumpArray();
                History.AddToForwardList(History.HistoryList[History.HistoryList.Count() - 1]);
                History.HistoryList.RemoveAt(History.HistoryList.Count() - 1);
                Debug.WriteLine("\nAfter Removals");
                ArrayDiag.DumpArray();
                ItemViewModel.FilesAndFolders.Clear();
                ItemViewModel.ViewModel = new ItemViewModel(History.HistoryList[History.HistoryList.Count() - 1]);     // To take into account the correct index without interference from the folder being navigated to
                GenericFileBrowser.P.path = History.HistoryList[History.HistoryList.Count() - 1];
                GenericFileBrowser.UpdateAllBindings();

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
            if (ItemViewModel.IsTerminated == false)
            {
                ItemViewModel.IsStopRequested = true;
            }

            if (History.ForwardList.Count() > 0)
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ItemViewModel.FilesAndFolders.Clear();
                ItemViewModel.ViewModel = new ItemViewModel(History.ForwardList[History.ForwardList.Count() - 1]);     // To take into account the correct index without interference from the folder being navigated to
                GenericFileBrowser.P.path = History.ForwardList[History.ForwardList.Count() - 1];
                History.ForwardList.RemoveAt(History.ForwardList.Count() - 1);
                GenericFileBrowser.UpdateAllBindings();
                ArrayDiag.DumpForwardArray();

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
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            ItemViewModel.FilesAndFolders.Clear();
            ItemViewModel.ViewModel = new ItemViewModel(ItemViewModel.PUIP.Path);
            GenericFileBrowser.P.path = ItemViewModel.PUIP.Path;
            GenericFileBrowser.UpdateAllBindings();
        }
    }

    public class UniversalPath : INotifyPropertyChanged
    {


        public string _path;
        public string path
        {
            get
            {
                return _path;
            }

            set
            {
                if (value != _path)
                {
                    _path = value;
                    NotifyPropertyChanged("path");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }
}