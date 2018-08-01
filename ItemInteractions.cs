//  ---- ItemInteractions.cs ----
//
//   Copyright 2018 Luke Blevins
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//  ---- This file contains code for user interaction with file system items ---- 
//


using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using System;
using Files;
using ItemListPresenter;
using Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.ComponentModel;
using System.Diagnostics;

namespace Interact
{



    public class Interaction
    {
        public static LargeImageSource lis = new LargeImageSource();
        public static LargeImageSource LIS { get { return lis; } }


        public static Page page;
        public Interaction(Page p)
        {
            page = p;
        }



        public static async void List_ItemClick(object sender, SelectionChangedEventArgs e)
        {
            if (page.Name == "GenericItemView")
            {


                if (e.AddedItems.Count == 1)
                {
                    var index = GenericFileBrowser.data.SelectedIndex;
                    var clickedOnItem = ItemViewModel.FilesAndFolders[index];


                    if (clickedOnItem.FileExtension == "Folder")
                    {
                        ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                        History.ForwardList.Clear();
                        ItemViewModel.FS.isEnabled = false;
                        ItemViewModel.FilesAndFolders.Clear();
                        ItemViewModel.ViewModel = new ItemViewModel(clickedOnItem.FilePath, false);
                        GenericFileBrowser.P.path = clickedOnItem.FilePath;
                        GenericFileBrowser.UpdateAllBindings();
                    }
                    else
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                        var options = new Windows.System.LauncherOptions();
                        options.DisplayApplicationPicker = true;
                        await Launcher.LaunchFileAsync(file, options);
                    }

                }
            }
           
        }

        public static void PhotoAlbumItemList_Click(object sender, ItemClickEventArgs e)
        {
            var index = PhotoAlbum.gv.SelectedIndex;
            var clickedOnItem = ItemViewModel.FilesAndFolders[index];
            Debug.WriteLine("Reached PhotoAlbumViewer event");

            if (clickedOnItem.FileExtension == "Folder")
            {

                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                History.ForwardList.Clear();
                ItemViewModel.FS.isEnabled = false;
                ItemViewModel.FilesAndFolders.Clear();
                ItemViewModel.ViewModel = new ItemViewModel(clickedOnItem.FilePath, true);
                GenericFileBrowser.P.path = clickedOnItem.FilePath;
                GenericFileBrowser.UpdateAllBindings();

            }
            else
            {
                /* StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                var options = new LauncherOptions();
                options.DisplayApplicationPicker = true;
                await Launcher.LaunchFileAsync(file, options); */
                var uri = new Uri(clickedOnItem.FilePath);
                BitmapImage bitmap = new BitmapImage();
                bitmap.UriSource = uri;
                LIS.image = bitmap;
                PhotoAlbum.largeImg.Source = bitmap;
            }
        }


        public static void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            DataGrid dataGrid = (DataGrid)sender;
            GenericFileBrowser.context.ShowAt(dataGrid, e.GetPosition(dataGrid));

        }

        public static async void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var ItemInvoked = sender as DataGridRow;
            var RowIndex = ItemInvoked.GetIndex();
            var RowData = ItemViewModel.FilesAndFolders[RowIndex];

            if (RowData.FileExtension == "Folder")
            {
                ItemViewModel.TextState.isVisible = Visibility.Collapsed;
                History.ForwardList.Clear();
                ItemViewModel.FS.isEnabled = false;
                ItemViewModel.FilesAndFolders.Clear();
                ItemViewModel.ViewModel = new ItemViewModel(RowData.FilePath, false);
                GenericFileBrowser.P.path = RowData.FilePath;
                GenericFileBrowser.UpdateAllBindings();
            }
            else
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(RowData.FilePath);
                var options = new LauncherOptions();
                options.DisplayApplicationPicker = true;
                await Launcher.LaunchFileAsync(file, options);
            }
        }

        public static void ShareItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public static void ScanItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public static void DeleteItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public static void RenameItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public static void CutItem_Click(object sender, RoutedEventArgs e)
        {

        }

        public static void CopyItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    

    public class LargeImageSource : INotifyPropertyChanged
    {


        public BitmapImage _image;
        public BitmapImage image
        {
            get
            {
                return _image;
            }

            set
            {
                if (value != _image)
                {
                    _image = value;
                    NotifyPropertyChanged("image");
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