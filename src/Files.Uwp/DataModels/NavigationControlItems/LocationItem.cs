using Files.Shared;
using Files.Uwp.Filesystem;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp;
using System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Files.Uwp.Helpers;

namespace Files.Uwp.DataModels.NavigationControlItems
{
    public class LocationItem : ObservableObject, INavigationControlItem
    {
        public BitmapImage icon;

        public BitmapImage Icon
        {
            get => icon;
            set => SetProperty(ref icon, value);
        }

        //public Uri IconSource { get; set; }
        public byte[] IconData { get; set; }

        public string Text { get; set; }

        private string path;

        public string Path
        {
            get => path;
            set
            {
                path = value;
                HoverDisplayText = string.IsNullOrEmpty(Path) || Path.Contains("?", StringComparison.Ordinal) || Path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) || Path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase) || Path == "Home".GetLocalized() ? Text : Path;
            }
        }

        public string HoverDisplayText { get; private set; }
        public FontFamily Font { get; set; }
        public NavigationControlItemType ItemType => NavigationControlItemType.Location;
        public bool IsDefaultLocation { get; set; }
        public BulkConcurrentObservableCollection<INavigationControlItem> ChildItems { get; set; }

        public bool SelectsOnInvoked { get; set; } = true;

        private bool isExpanded;
        public bool IsExpanded
        {
            get => isExpanded;
            set => SetProperty(ref isExpanded, value);
        }

        public bool IsInvalid { get; set; } = false;

        public SectionType Section { get; set; }

        public ContextMenuOptions MenuOptions { get; set; }

        public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);
    }
}