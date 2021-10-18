using Files.Filesystem.Search;
using Files.ViewModels.Search;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files.UserControls.Search
{
    public interface INavigator
    {
        void Clear();
        void GoRoot();
        void GoPage(object viewModel);
        void GoBack();
    }

    public class Navigator : INavigator
    {
        private readonly NavigationTransitionInfo emptyTransition =
            new SuppressNavigationTransitionInfo();
        private readonly NavigationTransitionInfo toRightTransition =
            new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };

        public static Navigator Instance { get; } = new Navigator();

        public Frame Frame { get; set; }

        private Navigator() {}

        public void Clear() => GoPage(null);
        public void GoRoot() => GoPage(new DateRangePageViewModel(null, new ModifiedFilter(DateRange.Always)));
        public void GoPage(object viewModel)
        {
            switch (viewModel)
            {
                //case ISettingsViewModel :
                //    Frame?.Navigate(typeof(SettingsPage), viewModel, emptyTransition);
                //    break;
                case IFilterPageViewModel :
                    Frame?.Navigate(typeof(FilterPage), viewModel, toRightTransition);
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
