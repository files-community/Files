using Files.Filesystem;
using Files.ViewModels.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Files.Layouts
{
    internal abstract class BaseListedLayout<TViewModel> : BaseLayout<TViewModel>
        where TViewModel : BaseListedLayoutViewModel
    {
        protected abstract ListViewBase FileListInternal { get; }

        protected virtual async void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await ViewModel.SelectionChanged(FileListInternal.SelectedItems.Cast<ListedItem>().Where((item) => item != null));
        }
    }
}
