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

using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace Files
{


    public sealed partial class YourHome : Page
    {

        string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        public YourHome()
        {
            this.InitializeComponent();
            
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
        }
    }
}
