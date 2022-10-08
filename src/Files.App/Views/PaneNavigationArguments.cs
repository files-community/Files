using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Filesystem;
using Files.App.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace Files.App.Views
{
    public class PaneNavigationArguments
    {
        public ListedItem Folder { get; }
        public IEnumerable<ListedItem> SelectedItems { get; }
        public LayoutModeArguments LayoutArguments { get; }
        public LayoutModeViewModel ViewModel { get; }

        public PaneNavigationArguments(ListedItem folder,
                                       IEnumerable<ListedItem>? selectedItems = null,
                                       LayoutModeArguments? layoutModeArguments = null)
        {
            Folder = folder;
            SelectedItems = selectedItems ?? Enumerable.Empty<ListedItem>();
            LayoutArguments = layoutModeArguments ?? new LayoutModeArguments();

            ViewModel = Ioc.Default.GetRequiredService<LayoutModeViewModel>();
            ViewModel.SelectedItems = SelectedItems;

        }
    }
}
