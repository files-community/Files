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
using Windows.ApplicationModel.DataTransfer;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage.Search;
using Windows.UI.Popups;

namespace Interact
{



    public class Interaction
    {
        


        public static Page page;
        public Interaction(Page p)
        {
            page = p;
        }

        public static MessageDialog message;
        private static Uri site_uri = new Uri(@"https://duke58701.wixsite.com/files-windows10/sideloading-help");

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
                    else if (clickedOnItem.FileExtension == "Executable")
                    {
                        message = new MessageDialog("We noticed you’re trying to run an executable file. This type of file may be a security risk to your device, and is not supported by the Universal Windows Platform. If you're not sure what this means, check out the Microsoft Store for a large selection of secure apps, games, and more.");
                        message.Title = "Unsupported Functionality";
                        message.Commands.Add(new UICommand("Continue...", new UICommandInvokedHandler(Interaction.CommandInvokedHandler)));
                        message.Commands.Add(new UICommand("Cancel"));
                        await message.ShowAsync();
                    }
                    else
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(clickedOnItem.FilePath);
                        var options = new LauncherOptions
                        {
                            DisplayApplicationPicker = true
                            
                        };
                        await Launcher.LaunchFileAsync(file, options);
                    }
                }
                else
                {
                    // Placeholder for row sorting logic
                }
                    

            }
           
        }

        private static async void CommandInvokedHandler(IUICommand command)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://home"));
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

        public static async void CopyItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            var DataGridSelectedItem = ItemViewModel.FilesAndFolders[GenericFileBrowser.data.SelectedIndex];
            if(DataGridSelectedItem != null)
            {
                var path = ItemViewModel.PUIP.Path;
                var fol = await StorageFolder.GetFolderFromPathAsync(path);
                var item = await fol.GetItemAsync(DataGridSelectedItem.FileName);
                List<IStorageItem> items = new List<IStorageItem>();
                items.Add(item);
                IEnumerable<IStorageItem> EnumerableOfItems = items;
                dataPackage.SetStorageItems(EnumerableOfItems);
                Clipboard.SetContent(dataPackage);
                
            }
        }

        public static async void PasteItem_ClickAsync(object sender, RoutedEventArgs e)
        {
            // TODO: Add progress box and collision for this operation
            var DestinationPath = ItemViewModel.PUIP.Path;
            DataPackageView packageView = Clipboard.GetContent();
            var ItemsToPaste = await packageView.GetStorageItemsAsync();
            foreach(IStorageItem item in ItemsToPaste)
            {
                StorageFolder SourceFolder = await StorageFolder.GetFolderFromPathAsync(item.Path);

                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    CloneDirectory(item.Path, DestinationPath);

                }
                else if (item.IsOfType(StorageItemTypes.File))
                {
                    StorageFile DestinationFile = await StorageFile.GetFileFromPathAsync(item.Path);
                    await DestinationFile.CopyAsync(await StorageFolder.GetFolderFromPathAsync(DestinationPath));
                }

            }

        }

        public static async void CloneDirectory(string root, string dest)
        {
            StorageFolder SourceFolder = await StorageFolder.GetFolderFromPathAsync(root);
            StorageFolder DestinationFolder = await StorageFolder.GetFolderFromPathAsync(dest);
            //// Check for clone of source folder in destination folder
            //var FolderCreate = await DestinationFolder.TryGetItemAsync(SourceFolder.Name);
            //// if not there, then create it
            //if (FolderCreate == null)
            //{
            //    await DestinationFolder.CreateFolderAsync(SourceFolder.Name);
            //    Debug.WriteLine("Source folder clone not found in destination");
            //}
            //// If there, then update dest folder to reflect this
            //else
            //{
            //    DestinationFolder = await StorageFolder.GetFolderFromPathAsync(dest + @"\" + SourceFolder.Name);
            //    foreach (var directory in await SourceFolder.GetFoldersAsync())
            //    {
            //        string DirName = directory.Name;
            //        if (await DestinationFolder.TryGetItemAsync(DirName) == null)
            //        {
            //            await DestinationFolder.CreateFolderAsync(DirName);
            //        }
            //        CloneDirectory(directory.Path, dest + @"\" + DirName);
            //    }
            //}

            var FolderCreate = await DestinationFolder.TryGetItemAsync(SourceFolder.Name);
            // Create initial root directory in dest if not there already
            if (FolderCreate == null)
            {
                await DestinationFolder.CreateFolderAsync(SourceFolder.Name);
                Debug.WriteLine("Source folder clone not found in destination");
            }
            foreach (var directory in await SourceFolder.GetFoldersAsync())
            {
                string DirName = directory.Name;
                if (await DestinationFolder.TryGetItemAsync(DirName) == null)
                {
                    await DestinationFolder.CreateFolderAsync(DirName);
                }
            }




        }
    }


}