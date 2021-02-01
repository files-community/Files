using ByteSizeLib;
using Files.Enums;
using Files.Filesystem;
using Files.Filesystem.Cloud;
using Files.Helpers;
using Files.UserControls.Selection;
using Files.View_Models;
using Files.Views;
using Files.Views.Pages;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Interaction = Files.Interacts.Interaction;

namespace Files
{
    public sealed partial class ColumnViewBrowser : BaseLayout
    {
        private string NavParam;

        public ColumnViewBrowser()
        {
            this.InitializeComponent();
            SelectedItemsPropertiesViewModel2 = SelectedItemsPropertiesViewModel;
            ColumnViewShellPage.SetSelectedItems += ColumnViewShellPage_SetSelectedItems;
            ColumnViewShellPage.NotifyRoot += ColumnViewShellPage_NotifyRoot;
            ColumnViewShellPage.GetCurrentColumnAndClearRest += ColumnViewShellPage_GetCurrentColumnAndClearRest; ;
        }

        private async void ColumnViewShellPage_GetCurrentColumnAndClearRest(object sender, EventArgs e)
        {
            var folder = sender as FolderInfo; 
            while (ColumnBladeView.Items.Count > folder.RootBladeNumber + 1)
            {
                try
                {
                    ColumnBladeView.Items.RemoveAt(folder.RootBladeNumber + 1);
                    ColumnBladeView.ActiveBlades.RemoveAt(folder.RootBladeNumber + 1);
                }
                catch
                {
                    break;
                }
            }
            await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(folder.Path);
            MainPage.MultitaskingControl?.UpdateSelectedTab(new DirectoryInfo(folder.Path).Name, folder.Path);
            await ParentShellPageInstance.FilesystemViewModel.EnumerateItemsFromStandardFolderAsync(folder.Path);
        }

        private async void ColumnViewShellPage_NotifyRoot(object sender, EventArgs e)
        {
            var folder = sender as FolderInfo;
            Frames = new List<Frame>();
            var frame = new Frame();
            Frames.Add(frame);
            while (ColumnBladeView.Items.Count > folder.RootBladeNumber + 1)
            {
                try 
                { 
                    ColumnBladeView.Items.RemoveAt(folder.RootBladeNumber + 1); 
                    ColumnBladeView.ActiveBlades.RemoveAt(folder.RootBladeNumber + 1);
                }
                catch 
                { 
                    break; 
                }
            }

            if (folder.Folder.ContainsFilesOrFolders)
            {
                var bi = new BladeItem
                {
                    Style = BladeStyle,
                    Content = frame
                };
                ColumnBladeView.Items.Add(bi);
                //ParentShellPageInstance.ServiceConnection.
                //App.Current.Suspending += Current_Suspending;
                var viewmodel = new ItemViewModel(ParentShellPageInstance);
                await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(folder.Folder.ItemPath);
                MainPage.MultitaskingControl?.UpdateSelectedTab(new DirectoryInfo(folder.Folder.ItemPath).Name, folder.Folder.ItemPath);
                await viewmodel.SetWorkingDirectoryAsync(folder.Folder.ItemPath);
                var InteractionOperations = new Interaction(ParentShellPageInstance);
                await ParentShellPageInstance.FilesystemViewModel.EnumerateItemsFromStandardFolderAsync(folder.Folder.ItemPath);
                //ParentShellPageInstance.FilesystemViewModel.RapidAddItemsToCollectionAsync(folder.Folder.ItemPath);
                viewmodel.AddItemsToCollectionAsync(folder.Folder.ItemPath);
                //Blah.ItemsSource = viewmodel.FilesAndFolders;
                frame.Navigate(typeof(ColumnViewShellPage), new ColumnViewNavParams
                {
                    Path = folder.Folder.ItemPath,
                    BladeNumber = ColumnBladeView.Items.IndexOf(bi),
                    CurrentInstance = ParentShellPageInstance,
                    SelectedItemsPropertiesViewModel = SelectedItemsPropertiesViewModel2,
                    //ItemsSource = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders,
                    ItemsSource = viewmodel.FilesAndFolders,
                    //ViewModel = ParentShellPageInstance.FilesystemViewModel,
                    ViewModel = viewmodel,
                    Interaction = InteractionOperations
                });
            }
        }

        private void ColumnViewShellPage_SetSelectedItems(object sender, EventArgs e)
        {
            var list = sender as List<ListedItem>;
            SelectedItems = list;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var param = (eventArgs.Parameter as NavigationArguments);
            NavParam = param.NavPathParam;
            if (this.IsLoaded)
            {
                Frames = new List<Frame>();
                var frame = new Frame();
                Frames.Add(frame);
                FirstBlade.Content = frame;
                try
                {
                    while (ColumnBladeView.ActiveBlades.Count > 1)
                    {
                        try
                        {
                            ColumnBladeView.Items.RemoveAt(1);
                            ColumnBladeView.ActiveBlades.RemoveAt(1);
                        }
                        catch
                        {
                            break;
                        }
                    }
                }
                catch
                {

                }
                //ParentShellPageInstance.ServiceConnection.
                //App.Current.Suspending += Current_Suspending;


                try
                {
                    ParentShellPageInstance.FilesystemViewModel.CancelLoadAndClearFiles();
                }
                catch
                {

                }
                var viewmodel = new ItemViewModel(ParentShellPageInstance);
                await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(NavParam);
                await viewmodel.SetWorkingDirectoryAsync(NavParam);
                var InteractionOperations = new Interaction(ParentShellPageInstance);
                await ParentShellPageInstance.FilesystemViewModel.EnumerateItemsFromStandardFolderAsync(NavParam);
                viewmodel.AddItemsToCollectionAsync(NavParam);
                //Blah.ItemsSource = viewmodel.FilesAndFolders;
                frame.Navigate(typeof(ColumnViewShellPage), new ColumnViewNavParams
                {
                    Path = NavParam,
                    BladeNumber = 0,
                    CurrentInstance = ParentShellPageInstance,
                    SelectedItemsPropertiesViewModel = SelectedItemsPropertiesViewModel2,
                    ItemsSource = viewmodel.FilesAndFolders,
                    ViewModel = viewmodel,
                    Interaction = InteractionOperations
                });
            }
        }
        private List<Frame> Frames;
        public static SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel2;

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Frames = new List<Frame>();
            var frame = new Frame();
            Frames.Add(frame);
            FirstBlade.Content = frame;
            try
            {
                while (ColumnBladeView.ActiveBlades.Count < 1)
                {
                    try
                    {
                        ColumnBladeView.Items.RemoveAt(1);
                        ColumnBladeView.ActiveBlades.RemoveAt(1);
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            catch
            {

            }
            var viewmodel = new ItemViewModel(ParentShellPageInstance);
            await viewmodel.SetWorkingDirectoryAsync(NavParam);
            await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(NavParam);
            await ParentShellPageInstance.FilesystemViewModel.EnumerateItemsFromStandardFolderAsync(NavParam);
            var InteractionOperations = new Interaction(ParentShellPageInstance);
            viewmodel.AddItemsToCollectionAsync(NavParam);
            //Blah.ItemsSource = viewmodel.FilesAndFolders;
            frame.Navigate(typeof(ColumnViewShellPage), new ColumnViewNavParams
            {
                Path = NavParam,
                BladeNumber = 0,
                CurrentInstance = ParentShellPageInstance,
                SelectedItemsPropertiesViewModel = SelectedItemsPropertiesViewModel2,
                ItemsSource = viewmodel.FilesAndFolders,
                ViewModel = viewmodel,
                Interaction = InteractionOperations
            });
            this.Loaded -= Page_Loaded;
        }

        public override void SelectAllItems()
        {
            // throw new NotImplementedException();
        }

        public override void InvertSelection()
        {
            // throw new NotImplementedException();
        }

        public override void ClearSelection()
        {
            // throw new NotImplementedException();
        }

        public override void SetDragModeForItems()
        {
            // throw new NotImplementedException();
        }

        public override void ScrollIntoView(ListedItem item)
        {
            // throw new NotImplementedException();
        }

        public override int GetSelectedIndex()
        {
            throw new NotImplementedException();
        }

        public override void SetSelectedItemOnUi(ListedItem selectedItem)
        {
            // throw new NotImplementedException();
        }

        public override void SetSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            // throw new NotImplementedException();
        }

        public override void AddSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            // throw new NotImplementedException();
        }

        public override void FocusSelectedItems()
        {
            // throw new NotImplementedException();
        }

        public override void StartRenameItem()
        {
            // throw new NotImplementedException();
        }

        public override void ResetItemOpacity()
        {
            // throw new NotImplementedException();
        }

        public override void SetItemOpacity(ListedItem item)
        {
            // throw new NotImplementedException();
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            throw new NotImplementedException();
        }
    }
}