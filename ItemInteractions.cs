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
        


        public static Page page;
        public Interaction(Page p)
        {
            page = p;
        }


        // Double-tap event for DataGrid
        public static async void List_ItemClick(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (page.Name == "GenericItemView")
            {
                
                var index = GenericFileBrowser.data.SelectedIndex;

                if(index > -1)
                {
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
                else
                {
                    // Placeholder for row sorting logic
                }
                    

            }
           
        }

        public static async void PhotoAlbumItemList_ClickAsync(object sender, ItemClickEventArgs e)
        {
            GridView grid = sender as GridView;
            var index = grid.Items.IndexOf(e.ClickedItem);
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
                StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                var options = new LauncherOptions();
                options.DisplayApplicationPicker = true;
                await Launcher.LaunchFileAsync(file, options); 
                //var uri = new Uri(clickedOnItem.FilePath);
                //BitmapImage bitmap = new BitmapImage();
                //bitmap.UriSource = uri;
                //LIS.image = bitmap;
                //PhotoAlbum.largeImg.Source = bitmap;
            }
        }


        public static void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            

            DataGrid dataGrid = (DataGrid)sender;
            
            // If user clicks on header
            if(dataGrid.CurrentColumn == null)
            {
                GenericFileBrowser.HeaderContextMenu.ShowAt(dataGrid, e.GetPosition(dataGrid));
            }
            // If user clicks on actual row
            else
            {
                GenericFileBrowser.context.ShowAt(dataGrid, e.GetPosition(dataGrid));
            }

        }

        public static void FileList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            GridView gridView = sender as GridView;
            PhotoAlbum.context.ShowAt(gridView, e.GetPosition(gridView));
        }

        public static async void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            var ItemSelected = GenericFileBrowser.data.SelectedIndex;
            var RowData = ItemViewModel.FilesAndFolders[ItemSelected];

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

    

    
}