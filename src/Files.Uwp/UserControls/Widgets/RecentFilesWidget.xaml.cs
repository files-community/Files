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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using Windows.UI.Core;
using System.Threading;
using System.Collections.Generic;

namespace Files.Uwp.UserControls.Widgets
{
    public sealed partial class RecentFilesWidget : UserControl, IWidgetItemModel
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        public delegate void RecentFilesOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);

        public event RecentFilesOpenLocationInvokedEventHandler RecentFilesOpenLocationInvoked;

        public delegate void RecentFileInvokedEventHandler(object sender, PathNavigationEventArgs e);

        public event RecentFileInvokedEventHandler RecentFileInvoked;

        private ObservableCollection<RecentItem> recentItemsCollection = new ObservableCollection<RecentItem>();

        private SemaphoreSlim refreshRecentsSemaphore;

        private CancellationTokenSource refreshRecentsCTS;

        private EmptyRecentsText Empty { get; set; } = new EmptyRecentsText();

        public string WidgetName => nameof(RecentFilesWidget);

        public string AutomationProperties => "RecentFilesWidgetAutomationProperties/Name".GetLocalized();

        public string WidgetHeader => "RecentFiles".GetLocalized();

        public bool IsWidgetSettingEnabled => UserSettingsService.WidgetsSettingsService.ShowRecentFilesWidget;

        public RecentFilesWidget()
        {
            InitializeComponent();

            refreshRecentsSemaphore = new SemaphoreSlim(1, 1);
            refreshRecentsCTS = new CancellationTokenSource();

            // recent files could have changed while widget wasn't loaded
            _ = App.RecentItemsManager.UpdateRecentFilesAsync();

            App.RecentItemsManager.RecentFilesChanged += Manager_RecentFilesChanged;
        }

        private async void Manager_RecentFilesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // e.Action can only be Reset right now; naively refresh everything for simplicity
                await UpdateRecentsList(e);
            });
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var flyoutItem = sender as MenuFlyoutItem;
            var clickedOnItem = flyoutItem.DataContext as RecentItem;
            if (clickedOnItem.IsFile)
            {
                var targetPath = clickedOnItem.RecentPath;
                RecentFilesOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
                {
                    ItemPath = Directory.GetParent(targetPath).FullName,    // parent directory
                    ItemName = Path.GetFileName(targetPath),                // file name w extension
                });
            }
        }

        private async Task UpdateRecentsList(NotifyCollectionChangedEventArgs args)
        {
            try
            {
                await refreshRecentsSemaphore.WaitAsync(refreshRecentsCTS.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                // drop other waiting instances
                refreshRecentsCTS.Cancel();
                refreshRecentsCTS = new CancellationTokenSource();

                Empty.Visibility = Visibility.Collapsed;

                switch (args.Action)
                {
                    // currently everything falls under Reset
                    default:
                        recentItemsCollection.Clear();
                        var recentFiles = App.RecentItemsManager.RecentFiles; // already sorted, add all in order
                        foreach (var recentFile in recentFiles)
                        {
                            await AddItemToRecentListAsync(recentFile);
                        }
                        break;
                }

                // update chevron if there aren't any items
                if (recentItemsCollection.Count == 0)
                {
                    Empty.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                App.Logger.Info(ex, "Could not populate recent files");
            }
            finally
            {
                refreshRecentsSemaphore.Release();
            }
        }

        /// <summary>
        /// Add the RecentItem to the ObservableCollection for the UI to render.
        /// </summary>
        /// <param name="recentItem">The recent item to be added</param>
        private async Task AddItemToRecentListAsync(RecentItem recentItem, bool sortInsert = false) 
        {
            await recentItem.LoadRecentItemIcon();
            recentItemsCollection.Add(recentItem);   
        }

        private void RecentsView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var path = (e.ClickedItem as RecentItem).RecentPath;
            RecentFileInvoked?.Invoke(this, new PathNavigationEventArgs()
            {
                ItemPath = path
            });
        }

        private async void RemoveRecentItem_Click(object sender, RoutedEventArgs e)
        {
            await refreshRecentsSemaphore.WaitAsync();

            try
            {
                // Get the sender FrameworkElement and grab its DataContext ViewModel
                if (sender is MenuFlyoutItem fe && fe.DataContext is RecentItem vm)
                {
                    // evict it from the recent items shortcut list
                    // this operation invokes RecentFilesChanged which we handle to update the visible collection
                    await App.RecentItemsManager.UnpinFromRecentFiles(vm.LinkPath);
                }
            }
            finally
            {
                refreshRecentsSemaphore.Release();
            }
        }

        private async void ClearRecentItems_Click(object sender, RoutedEventArgs e)
        {
            await refreshRecentsSemaphore.WaitAsync();
            try
            {
                recentItemsCollection.Clear();
                bool success = await App.RecentItemsManager.ClearRecentItems();

                if (success)
                {
                    Empty.Visibility = Visibility.Visible;
                }
            }
            finally
            {
                refreshRecentsSemaphore.Release();
            }
        }

        public async Task RefreshWidget()
        {
            // if files changed, event is fired to update widget
            await App.RecentItemsManager.UpdateRecentFilesAsync();
        }

        public void Dispose() 
        {
            App.RecentItemsManager.RecentFilesChanged -= Manager_RecentFilesChanged;
        }
    }

    public class EmptyRecentsText : INotifyPropertyChanged
    {
        private Visibility visibility;

        public Visibility Visibility
        {
            get
            {
                return visibility;
            }
            set
            {
                if (value != visibility)
                {
                    visibility = value;
                    NotifyPropertyChanged(nameof(Visibility));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}