using Files.Views.Pages;
using System.Collections.ObjectModel;

namespace Files.UserControls
{
    public interface INavigationToolbar
    {
        public bool IsSearchReigonVisible { get; set; }
        public bool IsEditModeEnabled { get; set; }
        public bool CanRefresh { get; set; }
        public bool CanNavigateToParent { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public string PathControlDisplayText { get; set; }
        public ObservableCollection<PathBoxItem> PathComponents { get; }
    }
}