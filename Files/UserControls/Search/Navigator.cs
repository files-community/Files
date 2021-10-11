using Files.ViewModels.Search;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files.UserControls.Search
{
    public class Navigator
    {
        private readonly NavigationTransitionInfo emptyTransition
            = new SuppressNavigationTransitionInfo();
        private readonly NavigationTransitionInfo toRightTransition
            = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };

        private readonly Frame frame;

        public INavigatorViewModel ViewModel { get; }

        public Navigator(Frame frame) : this(frame, NavigatorViewModel.Default)
        {
        }
        public Navigator(Frame frame, INavigatorViewModel viewModel)
        {
            this.frame = frame;
            ViewModel = viewModel;

            ViewModel.PageOpened += ViewModel_PageOpened;
            ViewModel.BackRequested += ViewModel_BackRequested;
            ViewModel.ForwardRequested += ViewModel_ForwardRequested;
        }

        public void GoRoot() => ViewModel.OpenPage(new SettingsViewModel());
        public void Clean() => ViewModel.OpenPage(null);

        private void ViewModel_PageOpened(INavigatorViewModel sender, PageOpenedNavigatorEventArgs e)
            => Go(e.ViewModel);
        private void ViewModel_BackRequested(INavigatorViewModel sender, EventArgs e)
            => frame.GoBack(toRightTransition);
        private void ViewModel_ForwardRequested(INavigatorViewModel sender, EventArgs e)
            => frame.GoForward();

        private void Go(object viewModel)
        {
            switch (viewModel)
            {
                case ISettingsViewModel :
                    frame.Navigate(typeof(SettingsPage), viewModel, emptyTransition);
                    break;
                case IFilterViewModel :
                    frame.Navigate(typeof(FilterPage), viewModel, toRightTransition);
                    break;
            }
        }
    }
}
