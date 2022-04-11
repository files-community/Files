using Files.Shared.Enums;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using Files.Backend.Services.Settings;
using Files.Uwp.ViewModels;
using Files.Uwp.ViewModels.Widgets;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Files.ViewModels.Widgets.RecentFiles;
using System.Linq;

namespace Files.Uwp.UserControls.Widgets
{
    public sealed partial class RecentFilesWidget : UserControl, IWidgetItemModel
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public delegate void RecentFilesOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);

        public event RecentFilesOpenLocationInvokedEventHandler RecentFilesOpenLocationInvoked;

        public delegate void RecentFileInvokedEventHandler(object sender, PathNavigationEventArgs e);

        public event RecentFileInvokedEventHandler RecentFileInvoked;

        public string WidgetName => nameof(RecentFilesWidget);

        public string AutomationProperties => "RecentFilesWidgetAutomationProperties/Name".GetLocalized();

        public string WidgetHeader => "RecentFiles".GetLocalized();

        public bool IsWidgetSettingEnabled => UserSettingsService.WidgetsSettingsService.ShowRecentFilesWidget;

        public RecentFilesWidgetViewModel ViewModel;

        public RecentFilesWidget()
        {
            InitializeComponent();
            ViewModel = new RecentFilesWidgetViewModel();
            DataContext = ViewModel;
            this.Loaded += RecentFilesWidget_Loaded;
        }

        private async void RecentFilesWidget_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= RecentFilesWidget_Loaded;
            await ViewModel.PopulateRecentsList();
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var flyoutItem = sender as MenuFlyoutItem;
            var clickedOnItem = flyoutItem.DataContext as ListedItem;
            if (clickedOnItem.PrimaryItemAttribute == StorageItemTypes.File)
            {
                var filePath = clickedOnItem.ItemPath;
                var folderPath = filePath.Substring(0, filePath.Length - clickedOnItem.ItemName.Length);
                RecentFilesOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
                {
                    ItemPath = folderPath,
                    ItemName = clickedOnItem.ItemName
                });
            }
        }

        private void RecentsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var path = (e.ClickedItem as ListedItem).ItemPath;
            RecentFileInvoked?.Invoke(this, new PathNavigationEventArgs()
            {
                ItemPath = path
            });
        }

        private async void RemoveRecentItem_Click(object sender, RoutedEventArgs e)
        {
            // Get the sender frameworkelement

            if (sender is MenuFlyoutItem fe)
            {
                // Grab it's datacontext ViewModel and remove it from the list.

                if (fe.DataContext is ListedItem vm)
                {
                    // Remove it from the visible collection
                    ViewModel.Items.Remove(vm);

                    // Now clear it from the recent list cache permanently.
                    // No token stored in the viewmodel, so need to find it the old fashioned way.
                    var mru = StorageApplicationPermissions.MostRecentlyUsedList;

                    foreach (var element in mru.Entries)
                    {
                        var f = await mru.GetItemAsync(element.Token);
                        if (f.Path == vm.ItemPath || element.Metadata == vm.ItemPath)
                        {
                            mru.Remove(element.Token);
                            ViewModel.IsRecentsListEmpty = !ViewModel.Items.Any();

                            break;
                        }
                    }
                }
            }
        }

        private void ClearRecentItems_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Items.Clear();
            RecentsView.ItemsSource = null;
            var mru = StorageApplicationPermissions.MostRecentlyUsedList;
            mru.Clear();
            ViewModel.IsRecentsListEmpty = !ViewModel.Items.Any();
        }

        public void Dispose()
        {
        }
    }

    //public class RecentItem
    //{
    //    public BitmapImage FileImg { get; set; }
    //    public string RecentPath { get; set; }
    //    public string Name { get; set; }
    //    public bool IsFile { get => Type == StorageItemTypes.File; }
    //    public StorageItemTypes Type { get; set; }
    //    public bool FolderImg { get; set; }
    //    public bool EmptyImgVis { get; set; }
    //    public bool FileIconVis { get; set; }
    //}
}