using Files.Filesystem;
using Files.Interacts;
using Files.UserControls;
using Files.View_Models;
using System;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public interface IShellPage
    {
        public StatusBarControl StatusBarControl { get; }
        public Frame ContentFrame { get; }
        public Interaction InteractionOperations { get; }
        public ItemViewModel FilesystemViewModel { get; }
        public CurrentInstanceViewModel InstanceViewModel { get; }
        public BaseLayout ContentPage { get; }
        public Control OperationsControl { get; }
        public Type CurrentPageType { get; }
        public INavigationControlItem SidebarSelectedItem { get; set; }
        public INavigationToolbar NavigationToolbar { get; }
    }
}