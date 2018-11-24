//  ---- MainPage.xaml.cs ----
//
//   Copyright 2018 Luke Blevins
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//  ---- This file contains various behind-the-scenes code for the navigation pane ---- 
//




using ItemListPresenter;
using System;
using System.ComponentModel;
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Files
{

    public sealed partial class MainPage : Page
    {
        public static NavigationView nv;
        string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        public MainPage()
        {
            this.InitializeComponent();
            this.IsTextScaleFactorEnabled = true;

            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(DragArea);
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Color.FromArgb(100, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            nv = navView;
            
        }

        private static SelectItem select = new SelectItem();
        public static SelectItem Select { get { return MainPage.select; } }

        private void navView_ItemSelected(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            NavigationViewItem item = args.SelectedItem as NavigationViewItem;
            
            if (item.Name == "homeIc")
            {
                ContentFrame.Navigate(typeof(YourHome));
                auto_suggest.PlaceholderText = "Search Recents";
            }
            else if (item.Name == "DesktopIC")
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
                auto_suggest.PlaceholderText = "Search Desktop";
            }
            else if (item.Name == "DocumentsIC")
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);
                auto_suggest.PlaceholderText = "Search Documents";
            }
            else if (item.Name == "DownloadsIC")
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);
                auto_suggest.PlaceholderText = "Search Downloads";
            }
            else if (item.Name == "PicturesIC")
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);
                auto_suggest.PlaceholderText = "Search Pictures";
            }
            else if (item.Name == "MusicIC")
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);
                auto_suggest.PlaceholderText = "Search Music";
            }
            else if (item.Name == "VideosIC")
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);
                auto_suggest.PlaceholderText = "Search Videos";
            }
            else if (item.Name == "LocD_IC")
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(GenericFileBrowser), @"C:\");
                auto_suggest.PlaceholderText = "Search";
            }
            else if (item.Name == "OneD_IC")
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                ContentFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);
                auto_suggest.PlaceholderText = "Search OneDrive";
            }
            else if(item.Content.Equals("Settings"))
            {
                ContentFrame.Navigate(typeof(Settings));
            }
        }

        
        

        private void auto_suggest_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            
        }

        private void navView_Loaded(object sender, RoutedEventArgs e)
        {
            
            foreach (NavigationViewItemBase item in navView.MenuItems)
            {
                if(item is NavigationViewItem && item.Name.ToString() == "homeIc")
                {
                    Select.itemSelected = item;
                    break;
                }
            }
            auto_suggest.IsEnabled = true;
            auto_suggest.PlaceholderText = "Search Recents";
        }
    }


    public class SelectItem : INotifyPropertyChanged
    {


        public NavigationViewItemBase _itemSelected;
        public NavigationViewItemBase itemSelected
        {
            get
            {
                return _itemSelected;
            }

            set
            {
                if (value != _itemSelected)
                {
                    _itemSelected = value;
                    NotifyPropertyChanged("itemSelected");
                    Debug.WriteLine("NotifyPropertyChanged was called successfully for NavView Selection");
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