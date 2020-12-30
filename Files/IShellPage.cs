using Files.Filesystem;
using Files.Interacts;
using Files.UserControls;
using Files.ViewModels;
using System;
using Windows.ApplicationModel.AppService;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public interface IShellPage : IDisposable
    {
        public StatusBarControl BottomStatusStripControl { get; }
        public Frame ContentFrame { get; }
        public Interaction InteractionOperations { get; }
        public ItemViewModel FilesystemViewModel { get; }
        public CurrentInstanceViewModel InstanceViewModel { get; }
        public AppServiceConnection ServiceConnection { get; }
        public BaseLayout ContentPage { get; }
        public Control OperationsControl { get; }
        public Type CurrentPageType { get; }
        public IFilesystemHelpers FilesystemHelpers { get; }
        public INavigationControlItem SidebarSelectedItem { get; set; }
        public INavigationToolbar NavigationToolbar { get; }
        public bool IsCurrentInstance { get; set; }

        public abstract void Clipboard_ContentChanged(object sender, object e);

        public abstract void Refresh_Click();
    }
}