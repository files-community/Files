//  ---- YourHome.xaml.cs ----
//
//   Copyright 2018 Luke Blevins
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//  ---- This file contains various behind-the-scenes code for the home page shortcuts ---- 
//

using ItemListPresenter;
using System;
using System.Drawing;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Files
{


    public sealed partial class YourHome : Page
    {
        public static string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        public static string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        public static string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public static string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public static string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        public YourHome()
        {
            this.InitializeComponent();
            Locations.ItemLoader.itemsAdded.Clear();
            Locations.ItemLoader.DisplayItems();
            SizeChanged += YourHome_SizeChanged;
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(Convert.ToInt32(bounds.Width * scaleFactor), Convert.ToInt32(bounds.Height * scaleFactor));
            // If width is between 1 - 800
            if (bounds.Width >= 1 && bounds.Width <= 800)
            {

            }
            else if (bounds.Width > 800 && bounds.Width <= 1024)
            {

            }
        }

        private void YourHome_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var bounds = ApplicationView.GetForCurrentView().VisibleBounds;
            var scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
            var size = new Size(Convert.ToInt32(bounds.Width * scaleFactor), Convert.ToInt32(bounds.Height * scaleFactor));
            // If width is between 1 - 800
            if (bounds.Width >= 1 && bounds.Width <= 800)
            {

            }
            else if (bounds.Width > 800 && bounds.Width <= 1024)
            {

            }
        }

        private void b0_Click(object sender, RoutedEventArgs e)
        {
            foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
            {
                if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "DesktopIC")
                {
                    MainPage.Select.itemSelected = NavItemChoice;
                    break;
                }
            }
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DesktopPath);
        }

        private void b1_Click(object sender, RoutedEventArgs e)
        {
            foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
            {
                if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "DownloadsIC")
                {
                    MainPage.Select.itemSelected = NavItemChoice;
                    break;
                }
            }
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DownloadsPath);

        }

        private void b2_Click(object sender, RoutedEventArgs e)
        {
            foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
            {
                if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "DocumentsIC")
                {
                    MainPage.Select.itemSelected = NavItemChoice;
                    break;
                }
            }
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), DocumentsPath);

        }

        private void b3_Click(object sender, RoutedEventArgs e)
        {
            foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
            {
                if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "PicturesIC")
                {
                    MainPage.Select.itemSelected = NavItemChoice;
                    break;
                }
            }
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            MainPage.accessibleContentFrame.Navigate(typeof(PhotoAlbum), PicturesPath);

        }

        private void b4_Click(object sender, RoutedEventArgs e)
        {
            foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
            {
                if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "MusicIC")
                {
                    MainPage.Select.itemSelected = NavItemChoice;
                    break;
                }
            }
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), MusicPath);

        }

        private void b5_Click(object sender, RoutedEventArgs e)
        {
            foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
            {
                if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "VideosIC")
                {
                    MainPage.Select.itemSelected = NavItemChoice;
                    break;
                }
            }
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), VideosPath);

        }

        private void b6_Click(object sender, RoutedEventArgs e)
        {
            foreach (NavigationViewItemBase NavItemChoice in MainPage.nv.MenuItems)
            {
                if (NavItemChoice is NavigationViewItem && NavItemChoice.Name.ToString() == "OneD_IC")
                {
                    MainPage.Select.itemSelected = NavItemChoice;
                    break;
                }
            }
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            MainPage.accessibleContentFrame.Navigate(typeof(GenericFileBrowser), OneDrivePath);

        }
    }
}
