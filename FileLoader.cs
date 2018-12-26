//  ---- FileLoader.cs ----
//
//   Copyright 2018 Luke Blevins
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//  ---- This file contains code for loading filesystem items ---- 
//

using System;
using Files;
using Navigation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Search;
using Windows.UI.Popups;
using Interacts;
using Windows.UI.Xaml.Controls;

namespace ItemListPresenter
{
    public class ListedItem
    {
        public Visibility FolderImg { get; set; }
        public Visibility FileIconVis { get; set; }
        public BitmapImage FileImg { get; set; }
        public string FileName { get; set; }
        public string FileDate { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
        public int ItemIndex { get; set; }
        public ListedItem()
        {

        }
    }

    public class Classic_ListedFolderItem
    {
        public string FileName { get; set; }
        public string FileDate { get; set; }
        public string FileExtension { get; set; }
        public string FilePath { get; set; }
        public ObservableCollection<Classic_ListedFolderItem> Children { get; set; } = new ObservableCollection<Classic_ListedFolderItem>();
    }

    public class ItemViewModel
    {
        public static ObservableCollection<Classic_ListedFolderItem> classicFolderList = new ObservableCollection<Classic_ListedFolderItem>();
        public static ObservableCollection<Classic_ListedFolderItem> ClassicFolderList { get { return classicFolderList; } }
        public ObservableCollection<ListedItem> classicFileList = new ObservableCollection<ListedItem>();
        public ObservableCollection<ListedItem> ClassicFileList { get { return classicFileList; } }

        public static ObservableCollection<ListedItem> filesAndFolders = new ObservableCollection<ListedItem>();
        public static ObservableCollection<ListedItem> FilesAndFolders { get { return filesAndFolders; } }

        string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string DocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string DownloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads";
        string OneDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive";
        string PicturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string MusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        string VideosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        StorageFolder folder;
        string gotName;
        string gotDate;
        string gotType;
        string gotPath;
        string gotFolName;
        string gotFolDate;
        string gotFolPath;
        string gotFolType;
        Visibility gotFileImgVis;
        Visibility gotFolImg;
        StorageItemThumbnail gotFileImg;
        public static ObservableCollection<Classic_ListedFolderItem> ChildrenList;
        public IReadOnlyList<StorageFolder> folderList;
        public IReadOnlyList<StorageFile> fileList;
        public bool isPhotoAlbumMode;
        public static string pageName;

        public static ItemViewModel vm;
        public static ItemViewModel ViewModel { get { return vm; } set { } }

        public static BackState bs = new BackState();
        public static BackState BS
        {
            get
            {
                return bs;
            }
            set
            {

            }
        }

        public static ForwardState fs = new ForwardState();
        public static ForwardState FS
        {
            get
            {
                return fs;
            }
            set
            {

            }
        }

        public static ProgressUIVisibility pvis = new ProgressUIVisibility();
        public static ProgressUIVisibility PVIS
        {
            get
            {
                return pvis;
            }
            set
            {

            }
        }

        public ItemViewModel(string ViewPath, Page p)
        {
            pageName = p.Name;
            // Personalize retrieved items for view they are displayed in
            if(p.Name == "GenericItemView" || p.Name == "ClassicModePage")
            {
                isPhotoAlbumMode = false;
            }
            else if (p.Name == "PhotoAlbumViewer")
            {
                isPhotoAlbumMode = true;
            }
            
            if(pageName != "ClassicModePage")
            {
                GenericFileBrowser.P.path = ViewPath;
                FilesAndFolders.Clear();
            }
                
            GetItemsAsync(ViewPath);

            if (pageName != "ClassicModePage")
            {
                History.AddToHistory(ViewPath);

                if (History.HistoryList.Count == 1)
                {
                    BS.isEnabled = false;
                    //Debug.WriteLine("Disabled Property");


                }
                else if (History.HistoryList.Count > 1)
                {
                    BS.isEnabled = true;
                    //Debug.WriteLine("Enabled Property");
                }
            }
            

        }

        private async void DisplayConsentDialog()
        {
            MessageDialog message = new MessageDialog("This app is not able to access your files. You need to allow it to by granting permission in Settings.");
            message.Title = "Permission Denied";
            message.Commands.Add(new UICommand("Allow...", new UICommandInvokedHandler(Interaction.GrantAccessPermissionHandler)));
            await message.ShowAsync();
        }

        private ListedItem li = new ListedItem();
        public ListedItem LI { get { return this.li; } }

        private static ProgressUIHeader pUIh = new ProgressUIHeader();
        public static ProgressUIHeader PUIH { get { return ItemViewModel.pUIh; } }

        private static ProgressUIPath pUIp = new ProgressUIPath();
        public static ProgressUIPath PUIP { get { return ItemViewModel.pUIp; } }

        private static ProgressUIButtonText buttonText = new ProgressUIButtonText();
        public static ProgressUIButtonText ButtonText { get { return ItemViewModel.buttonText; } }

        private static CollisionBoxHeader collisionBoxHeader = new CollisionBoxHeader();
        public static CollisionBoxHeader CollisionBoxHeader { get { return collisionBoxHeader; } }

        private static CollisionBoxSubHeader collisionBoxSubHeader = new CollisionBoxSubHeader();
        public static CollisionBoxSubHeader CollisionBoxSubHeader { get { return collisionBoxSubHeader; } }

        private static CollisionUIVisibility collisionUIVisibility = new CollisionUIVisibility();
        public static CollisionUIVisibility CollisionUIVisibility { get { return collisionUIVisibility; } }

        private static CollisionBoxHeader conflictBoxHeader = new CollisionBoxHeader();
        public static CollisionBoxHeader ConflictBoxHeader { get { return conflictBoxHeader; } }

        private static CollisionBoxSubHeader conflictBoxSubHeader = new CollisionBoxSubHeader();
        public static CollisionBoxSubHeader ConflictBoxSubHeader { get { return conflictBoxSubHeader; } }

        private static CollisionUIVisibility conflictUIVisibility = new CollisionUIVisibility();
        public static CollisionUIVisibility ConflictUIVisibility { get { return conflictUIVisibility; } }

        private static EmptyFolderTextState textState = new EmptyFolderTextState();
        public static EmptyFolderTextState TextState { get { return textState; } }
        public static bool IsStopRequested = false;
        public static bool IsTerminated = true;

        public async void GetItemsAsync(string path)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            IsTerminated = false;
            PUIP.Path = path;
            try
            {
                folder = await StorageFolder.GetFolderFromPathAsync(path);          // Set location to the current directory specified in path
                folderList = await folder.GetFoldersAsync();                        // Create a read-only list of all folders in location
                fileList = await folder.GetFilesAsync();                            // Create a read-only list of all files in location
                int NumOfFolders = folderList.Count;                                // How many folders are in the list
                int NumOfFiles = fileList.Count;                                    // How many files are in the list
                int NumOfItems = NumOfFiles + NumOfFolders;
                int NumItemsRead = 0;

                if (NumOfItems == 0)
                {
                    TextState.isVisible = Visibility.Visible;
                    //return;
                }

                PUIH.Header = "Loading " + NumOfItems + " items";
                ButtonText.buttonText = "Hide";
                
                if (NumOfItems >= 250)
                {
                    PVIS.isVisible = Visibility.Visible;
                }

                if(NumOfFolders > 0)
                {
                    foreach (StorageFolder fol in folderList)
                    {
                        if(IsStopRequested)
                        {
                            IsStopRequested = false;
                            IsTerminated = true;
                            return;
                        }
                        int ProgressReported = (NumItemsRead * 100 / NumOfItems);
                        UpdateProgUI(ProgressReported);
                        gotFolName = fol.Name.ToString();
                        gotFolDate = fol.DateCreated.ToString();
                        gotFolPath = fol.Path.ToString();
                        gotFolType = "Folder";
                        gotFolImg = Visibility.Visible;
                        gotFileImgVis = Visibility.Collapsed;
                        

                        if (pageName == "ClassicModePage")
                        {
                            ClassicFolderList.Add(new Classic_ListedFolderItem() { FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                        }
                        else
                        {
                            FilesAndFolders.Add(new ListedItem() { ItemIndex = FilesAndFolders.Count, FileImg = null, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotFolName, FileDate = gotFolDate, FileExtension = gotFolType, FilePath = gotFolPath });
                        }


                        NumItemsRead++;
                    }
                
                }

                if(NumOfFiles > 0)
                {
                    foreach (StorageFile f in fileList)
                    {
                        if (IsStopRequested)
                        {
                            IsStopRequested = false;
                            IsTerminated = true;
                            return;
                        }
                        int ProgressReported = (NumItemsRead * 100 / NumOfItems);
                        UpdateProgUI(ProgressReported);
                        gotName = f.Name.ToString();
                        gotDate = f.DateCreated.ToString(); // In the future, parse date to human readable format
                        if(f.FileType.ToString() == ".exe")
                        {
                            gotType = "Executable";
                        }
                        else
                        {
                            gotType = f.DisplayType;
                        }
                        gotPath = f.Path.ToString();
                        gotFolImg = Visibility.Collapsed;
                        if (isPhotoAlbumMode == false)
                        {
                            const uint requestedSize = 20;
                            const ThumbnailMode thumbnailMode = ThumbnailMode.ListView;
                            const ThumbnailOptions thumbnailOptions = ThumbnailOptions.UseCurrentScale;
                            gotFileImg = await f.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                        }
                        else
                        {
                            const uint requestedSize = 275;
                            const ThumbnailMode thumbnailMode = ThumbnailMode.PicturesView;
                            const ThumbnailOptions thumbnailOptions = ThumbnailOptions.ResizeThumbnail;
                            gotFileImg = await f.GetThumbnailAsync(thumbnailMode, requestedSize, thumbnailOptions);
                        }

                        BitmapImage icon = new BitmapImage();
                        if (gotFileImg != null)
                        {
                            icon.SetSource(gotFileImg.CloneStream());
                        }
                        gotFileImgVis = Visibility.Visible;

                        if(pageName == "ClassicModePage")
                        {
                            ClassicFileList.Add(new ListedItem() { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                        }
                        else
                        {
                            FilesAndFolders.Add(new ListedItem() { FileImg = icon, FileIconVis = gotFileImgVis, FolderImg = gotFolImg, FileName = gotName, FileDate = gotDate, FileExtension = gotType, FilePath = gotPath });
                        }
                        NumItemsRead++;
                    }

                
                }
                if(pageName != "ClassicModePage")
                {
                    PVIS.isVisible = Visibility.Collapsed;
                }
                
                IsTerminated = true;
            }
            catch (UnauthorizedAccessException)
            {
                DisplayConsentDialog();
            }
            stopwatch.Stop();
            Debug.WriteLine("Loading of: " + path + " completed in " + stopwatch.ElapsedMilliseconds + " Milliseconds.");

        }


        public static ProgressPercentage progressPER = new ProgressPercentage();

        public static ProgressPercentage PROGRESSPER
        {
            get
            {
                return progressPER;
            }
            set
            {

            }
        }

        public static int UpdateProgUI(int level)
        {
            PROGRESSPER.prog = level;
            return (int)level;
        }

        public static void DisplayCollisionUIWithArgs(string header, string subHeader)
        {
            CollisionBoxHeader.Header = header;
            CollisionBoxSubHeader.SubHeader = subHeader;
            CollisionUIVisibility.isVisible = Visibility.Visible;
        }

        public static void DisplayReviewUIWithArgs(string header, string subHeader)
        {
            ConflictBoxHeader.Header = header;
            ConflictBoxSubHeader.SubHeader = subHeader;
            ConflictUIVisibility.isVisible = Visibility.Visible;
        }

        public static void DisplayProgUIWithArgs(string headerText, string messageText, string buttonText, int initialProgBarLevel)
        {
            PUIH.Header = headerText;
            PUIP.Path = messageText;
            ButtonText.buttonText = buttonText;
            PROGRESSPER.prog = initialProgBarLevel;
            PVIS.isVisible = Visibility.Visible;
        }

        public static async void FillTreeNode(object item, TreeView EntireControl)
        {
            var PathToFillFrom = (item as Classic_ListedFolderItem).FilePath;
            StorageFolder FolderFromPath = await StorageFolder.GetFolderFromPathAsync(PathToFillFrom);
            IReadOnlyList<StorageFolder> SubFolderList = await FolderFromPath.GetFoldersAsync();
            foreach(StorageFolder fol in SubFolderList)
            {
                var name = fol.Name;
                var date = fol.DateCreated.LocalDateTime.ToString();
                var ext = fol.DisplayType;
                var path = fol.Path;
                (item as Classic_ListedFolderItem).Children.Add(new Classic_ListedFolderItem() { FileName = name, FilePath = path, FileDate = date, FileExtension = ext});

            }
        }
    }
}
