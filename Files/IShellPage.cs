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
    public interface IShellPage : ITabItemContent, IMultiPaneInfo, IDisposable
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
        public INavigationToolbar NavigationToolbar { get; }

        public abstract void Refresh_Click();
        public void UpdatePathUIToWorkingDirectory(string newWorkingDir, string singleItemOverride = null);
        public void NavigateToPath(string navigationPath, Type sourcePageType);
    }

    public interface IPaneHolder : IDisposable
    {
        public void OpenPathInNewPane(string path);
    }

    public interface IMultiPaneInfo
    {
        public bool IsPageMainPane { get; } // The instance is the left (or only) pane
        public bool IsMultiPaneActive { get; } // Another pane is shown
        public bool IsMultiPaneEnabled { get; } // Multi pane is enabled
        public IPaneHolder PaneHolder { get; }
    }
}