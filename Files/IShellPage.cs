using Files.Filesystem;
using Files.Interacts;
using Files.UserControls;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using System;
using Windows.ApplicationModel.AppService;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public interface IShellPage : ITabItemContent, IDisposable
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
        public INavigationControlItem SidebarSelectedItem { get; set; }
        public INavigationToolbar NavigationToolbar { get; }
        
        public bool IsPageMainPane { get; }
        public IPaneHolder PaneHolder { get; }

        public abstract void Clipboard_ContentChanged(object sender, object e);

        public abstract void Refresh_Click();
    }

    public interface IPaneHolder : IDisposable
    {
        public void OpenPathInNewPane(string path);
    }
}