//  ---- GenericFileBrowser.xaml.cs ----
//
//   Copyright 2018 Luke Blevins
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//  ---- This file contains various behind-the-scenes code for the regular item layout ---- 
//


using ItemListPresenter;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Navigation;
using System;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Files
{

    public sealed partial class GenericFileBrowser : Page
    {
        public TextBlock textBlock;
        public static DataGrid data;
        public static MenuFlyout context;
        public static MenuFlyout HeaderContextMenu;

        public GenericFileBrowser()
        {
            this.InitializeComponent();

            string env = Environment.ExpandEnvironmentVariables("%userprofile%");

            this.IsTextScaleFactorEnabled = true;
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Color.FromArgb(100, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            ProgressBox.Visibility = Visibility.Collapsed;
            ItemViewModel.TextState.isVisible = Visibility.Collapsed;
            ItemViewModel.PVIS.isVisible = Visibility.Collapsed;
            data = AllView;
            context = RightClickContextMenu;
            HeaderContextMenu = HeaderRightClickMenu;
            Interact.Interaction.page = this;
            OpenItem.Click += Interact.Interaction.OpenItem_Click;
            ShareItem.Click += Interact.Interaction.ShareItem_Click;
            ScanItem.Click += Interact.Interaction.ScanItem_Click;
            DeleteItem.Click += Interact.Interaction.DeleteItem_Click;
            RenameItem.Click += Interact.Interaction.RenameItem_Click;
            CutItem.Click += Interact.Interaction.CutItem_Click;
            CopyItem.Click += Interact.Interaction.CopyItem_Click;
            AllView.RightTapped += Interact.Interaction.AllView_RightTapped;
            Back.Click += Navigation.NavigationActions.Back_Click;
            Forward.Click += Navigation.NavigationActions.Forward_Click;
            Refresh.Click += Navigation.NavigationActions.Refresh_Click;
            AllView.DoubleTapped += Interact.Interaction.List_ItemClick;
            
        }

        

        public static UniversalPath p = new UniversalPath();
        public static UniversalPath P { get { return GenericFileBrowser.p; } }

        public static GenericFileBrowser getGenericFileBrowser = new GenericFileBrowser();
        public static GenericFileBrowser GetGenericFileBrowser { get { return getGenericFileBrowser; } }

        public static void UpdateAllBindings()
        {
            GetGenericFileBrowser.Bindings.Update();
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var parameters = (string)eventArgs.Parameter;
            ItemViewModel.ViewModel = new ItemViewModel(parameters, false);
            if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
            {
                P.path = "Desktop";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
            {
                P.path = "Documents";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads"))
            {
                P.path = "Downloads";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
            {
                P.path = "Pictures";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
            {
                P.path = "Music";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\OneDrive"))
            {
                P.path = "OneDrive";
            }
            else if (parameters.Equals(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            {
                P.path = "Videos";
            }
            else
            {
                P.path = parameters;
            }

        }




        private void AllView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            
        }

        private async void AllView_DropAsync(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if(items.Count() == 1)
                {
                    DataPackage data = new DataPackage();
                    foreach(IStorageItem storageItem in items)
                    {
                        var itemPath = storageItem.Path;

                    } 
                }
            }
        }

        // Click event for Hide button on Progress UI Synthetic Dialog 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ProgressBox.Visibility = Visibility.Collapsed;
        }

       
    }

    public class EmptyFolderTextState : INotifyPropertyChanged
    {


        public Visibility _isVisible;
        public Visibility isVisible
        {
            get
            {
                return _isVisible;
            }

            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    NotifyPropertyChanged("isVisible");
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