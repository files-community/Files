using Files.DataModels.NavigationControlItems;
using Files.Backend.Services.Settings;
using Files.UserControls;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class FileTagsManager
    {
        private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>();

        private IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>();

        public async Task EnumerateFileTagsAsync()
        {
            try
            {
                await SyncSideBarItemsUI();
            }
            catch (Exception) // UI Thread not ready yet, so we defer the pervious operation until it is.
            {
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet
                CoreApplication.MainView.Activated += EnumerateFileTagsAsync;
            }
        }

        private async void EnumerateFileTagsAsync(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            await SyncSideBarItemsUI();
            CoreApplication.MainView.Activated -= EnumerateFileTagsAsync;
        }

        private async Task SyncSideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await SidebarControl.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    var section = SidebarControl.SideBarItems.FirstOrDefault(x => x.Text == "FileTags".GetLocalized()) as LocationItem;
                    if (UserSettingsService.PreferencesSettingsService.AreFileTagsEnabled && UserSettingsService.AppearanceSettingsService.ShowFileTagsSection && section == null)
                    {
                        section = new LocationItem()
                        {
                            Text = "FileTags".GetLocalized(),
                            Section = SectionType.FileTag,
                            SelectsOnInvoked = false,
                            Icon = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/FluentIcons/FileTags.png")),
                            ChildItems = new ObservableCollection<INavigationControlItem>()
                        };
                        var index = (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Favorites) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Library) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Drives) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.CloudDrives) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.Network) ? 1 : 0) +
                                    (SidebarControl.SideBarItems.Any(item => item.Section == SectionType.WSL) ? 1 : 0); // After wsl section
                        SidebarControl.SideBarItems.BeginBulkOperation();
                        SidebarControl.SideBarItems.Insert(Math.Min(index, SidebarControl.SideBarItems.Count), section);
                        SidebarControl.SideBarItems.EndBulkOperation();
                    }

                    if (section != null)
                    {
                        foreach (var tag in FileTagsSettingsService.FileTagList)
                        {
                            if (!section.ChildItems.Any(x => x.Path == $"tag:{tag.TagName}"))
                            {
                                section.ChildItems.Add(new FileTagItem()
                                {
                                    Text = tag.TagName,
                                    Path = $"tag:{tag.TagName}",
                                    FileTag = tag
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.Warn(ex, "Error loading tags section.");
                }
                finally
                {
                    SidebarControl.SideBarItemsSemaphore.Release();
                }
            });
        }

        private void RemoveFileTagsSideBarSection()
        {
            try
            {
                var item = (from n in SidebarControl.SideBarItems where n.Text.Equals("FileTags".GetLocalized()) select n).FirstOrDefault();
                if (!UserSettingsService.AppearanceSettingsService.ShowFileTagsSection && item != null)
                {
                    SidebarControl.SideBarItems.Remove(item);
                }
            }
            catch (Exception)
            { }
        }

        public async void UpdateFileTagsSectionVisibility()
        {
            if (UserSettingsService.AppearanceSettingsService.ShowFileTagsSection)
            {
                await EnumerateFileTagsAsync();
            }
            else
            {
                RemoveFileTagsSideBarSection();
            }
        }
    }
}
