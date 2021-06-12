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
    public interface IShellPage : ITabItemContent, IMultiPaneInfo, IDisposable
    {
        ItemViewModel FilesystemViewModel { get; }

        CurrentInstanceViewModel InstanceViewModel { get; }

        NamedPipeAsAppServiceConnection ServiceConnection { get; }

        IBaseLayout SlimContentPage { get; }

        Type CurrentPageType { get; }

        IFilesystemHelpers FilesystemHelpers { get; }

        NavToolbarViewModel NavToolbarViewModel { get; }

        bool CanNavigateBackward { get; }

        bool CanNavigateForward { get; }

        void Refresh_Click();

        void UpdatePathUIToWorkingDirectory(string newWorkingDir, string singleItemOverride = null);

        void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null);
        /// <summary>
        /// Gets the layout mode for the specified path then navigates to it
        /// </summary>
        /// <param name="navigationPath"></param>
        /// <param name="navArgs"></param>
        public void NavigateToPath(string navigationPath, NavigationArguments navArgs = null);
        /// <summary>
        /// Navigates to the home page
        /// </summary>
        public void NavigateHome();

        void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs);

        void RemoveLastPageFromBackStack();

        void SubmitSearch(string query, bool searchUnindexedItems);

        void LoadPreviewPaneChanged();
    }

    public interface IPaneHolder : IDisposable
    {
        public event EventHandler ActivePaneChanged;

        public IShellPage ActivePane { get; set; }
        public IFilesystemHelpers FilesystemHelpers { get; }
        public TabItemArguments TabItemArguments { get; set; }

        public void OpenPathInNewPane(string path);
        public bool IsLeftPaneActive { get; }
        public bool IsRightPaneActive { get; }
    }

    public interface IMultiPaneInfo
    {
        public bool IsPageMainPane { get; } // The instance is the left (or only) pane
        public bool IsMultiPaneActive { get; } // Another pane is shown
        public bool IsMultiPaneEnabled { get; } // Multi pane is enabled
        public IPaneHolder PaneHolder { get; }
    }
}