using Files.ViewModels.Search;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files.UserControls.Search
{
    public interface ISearchNavigator
    {
        void Search();
        void Back();
        void GoPage(object viewModel);
    }

    public class SearchNavigator : ISearchNavigator
    {
        private readonly NavigationTransitionInfo emptyTransition =
            new SuppressNavigationTransitionInfo();
        private readonly NavigationTransitionInfo toRightTransition =
            new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };

        public ISearchBox SearchBox { get; set; }
        public Frame Frame { get; set; }

        public void Search()
        {
            if (SearchBox is not null)
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Query))
                {
                    SearchBox.Query = "*";
                }
                SearchBox.Search();
                SearchBox.IsMenuOpen = false;
            }
        }

        public void Back()
        {
            if (Frame is not null && Frame.CanGoBack)
            {
                Frame.GoBack(toRightTransition);
            }
        }

        public void GoPage(object viewModel)
        {
            if (Frame is null)
            {
                return;
            }
            switch (viewModel)
            {
                case ISearchSettingsViewModel:
                    Frame.Navigate(typeof(SearchSettingsPage), viewModel, emptyTransition);
                    break;
                case IMultiSearchPageViewModel:
                    Frame.Navigate(typeof(MultiFilterPage), viewModel, toRightTransition);
                    break;
                case ISearchPageViewModel:
                    Frame.Navigate(typeof(FilterPage), viewModel, toRightTransition);
                    break;
                default:
                    Frame.Content = null;
                    break;
            }
        }
    }
}
