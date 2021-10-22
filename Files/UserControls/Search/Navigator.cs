using Files.Filesystem.Search;
using Files.ViewModels.Search;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files.UserControls.Search
{
    public interface INavigator
    {
        ObservableCollection<IFilter> CurrentCollection { get; }

        void Clear();
        void GoRoot();
        void GoPage(IFilterPageViewModel viewModel);
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

        public ObservableCollection<IFilter> CurrentCollection
        {
            get
            {
                int count = Frame.BackStack.Count;
                if (count == 0)
                {
                    return null;
                }
                var parentViewModel = Frame.BackStack[count - 1].Parameter as IGroupPageViewModel;
                return parentViewModel?.Picker?.Filters;
            }
        }

        private Navigator() {}

        public void Clear()
        {
            GoPage(null);
        }

        public void GoRoot()
        {
            GoPage(new GroupPageViewModel(new AndFilterCollection()));
        }
        public void GoPage(IFilterPageViewModel viewModel)
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
                case IMultiFilterPageViewModel :
                    Frame.Navigate(typeof(MultiFilterPage), viewModel, toRightTransition);
                    break;
                case IFilterPageViewModel:
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
