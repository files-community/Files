using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Files.UserControls.MultitaskingControl;
using Files.ViewModels;
using Files.Views;
using System;

namespace Files
{
    public interface IMultiPaneInfo
    {
        public bool IsMultiPaneActive { get; }

        // Another pane is shown
        public bool IsMultiPaneEnabled { get; }

        public bool IsPageMainPane { get; } // The instance is the left (or only) pane

        // Multi pane is enabled
        public IPaneHolder PaneHolder { get; }
    }

    public interface IPaneHolder : IDisposable
    {
        public event EventHandler ActivePaneChanged;

        public IShellPage ActivePane { get; set; }
        public IFilesystemHelpers FilesystemHelpers { get; }
        public TabItemArguments TabItemArguments { get; set; }

        public void OpenPathInNewPane(string path);
    }

    public interface IShellPage : ITabItemContent, IMultiPaneInfo, IDisposable
    {
        bool CanNavigateBackward { get; }
        bool CanNavigateForward { get; }
        Type CurrentPageType { get; }
        IFilesystemHelpers FilesystemHelpers { get; }
        ItemViewModel FilesystemViewModel { get; }

        //Interaction InteractionOperations { get; }
        CurrentInstanceViewModel InstanceViewModel { get; }

        INavigationToolbar NavigationToolbar { get; }
        NamedPipeAsAppServiceConnection ServiceConnection { get; }
        IBaseLayout SlimContentPage { get; }
        IStatusCenterActions StatusCenterActions { get; }

        void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null);

        void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs);

        void Refresh_Click();

        void RemoveLastPageFromBackStack();

        void SubmitSearch(string query, bool searchUnindexedItems);

        void UpdatePathUIToWorkingDirectory(string newWorkingDir, string singleItemOverride = null);
    }
}