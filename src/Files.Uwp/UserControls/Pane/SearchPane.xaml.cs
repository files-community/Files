using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Filesystem.Search;
using Files.UserControls.Search;
using Files.ViewModels.Search;
using Windows.UI.Xaml.Controls;

namespace Files.UserControls
{
    public sealed partial class SearchPane : UserControl, IPane
    {
        public PanePositions Position { get; } = PanePositions.Right;

        private ISearchNavigator Navigator { get; } = Ioc.Default.GetService<ISearchNavigator>();

        public SearchPane()
        {
            InitializeComponent();

            var settings = Ioc.Default.GetService<ISearchSettings>();

            Navigator.Initialize(MenuFrame);
            Navigator.GoPage(new SearchSettingsViewModel(settings));
        }

        public void UpdatePosition(double panelWidth, double panelHeight) {}

    }
}
