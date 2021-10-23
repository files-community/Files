using Files.ViewModels.Search;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files.UserControls.Search
{
    public interface INavigator
    {
        void Clear();
        void Search();

        void GoPage(ISearchPageViewModel viewModel);
        void GoBack();
    }

    public class Navigator : INavigator
    {
        private readonly NavigationTransitionInfo emptyTransition =
            new SuppressNavigationTransitionInfo();
        private readonly NavigationTransitionInfo toRightTransition =
            new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };

        public Frame Frame { get; set; }

        public void Clear() => GoPage(null);
        public void Search() {}

        public void GoPage(ISearchPageViewModel viewModel)
        {
            if (Frame is null)
            {
                return;
            }
            switch (viewModel)
            {
                //case ISettingsViewModel :
                //    Frame?.Navigate(typeof(SettingsPage), viewModel, emptyTransition);
                //    break;
                case IMultiSearchPageViewModel :
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
        public void GoBack()
        {
            if (Frame is not null && Frame.CanGoBack)
            {
                Frame.GoBack(toRightTransition);
            }
        }
    }
}
