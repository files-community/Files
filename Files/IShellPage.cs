using Files.Filesystem;
using Files.Interacts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Files
{
    public interface IShellPage
    {
        public Frame ContentFrame { get; }
        public Interaction InteractionOperations { get; }
        public ItemViewModel ViewModel { get; }
        public BaseLayout ContentPage { get; }
        public Control OperationsControl { get; }
        public bool CanRefresh { get; set; }
        public bool CanNavigateToParent { get; set; }
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public string PathControlDisplayText { get; set; }
        public ObservableCollection<PathBoxItem> PathComponents { get; }
        public Type CurrentPageType { get; }
    }
}
