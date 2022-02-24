using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.ViewModels.Search;
using System.ComponentModel;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Files.UserControls.Search
{
    public interface ISearchNavigator : INotifyPropertyChanged
    {
        ISearchPageViewModel PageViewModel { get; }

        ICommand SearchCommand { get; }
        ICommand BackCommand { get; }

        void Initialize(Frame frame);
        void Initialize(ISearchBox box);

        void Search();
        void Back();

        void ClearPage();
        void GoPage(ISearchFilterViewModel filter);
    }

    public class SearchNavigator : ObservableObject, ISearchNavigator
    {
        private readonly ISearchPageViewModelFactory viewModelFactory =
            Ioc.Default.GetService<ISearchPageViewModelFactory>();

        private readonly NavigationTransitionInfo transition =
            new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };

        private ISearchPageViewModel pageViewModel;
        public ISearchPageViewModel PageViewModel => pageViewModel;

        public ICommand SearchCommand { get; }

        private readonly RelayCommand backCommand;
        public ICommand BackCommand => backCommand;

        private ISearchBox box;
        private Frame frame;

        public SearchNavigator()
        {
            SearchCommand = new RelayCommand(Search);
            backCommand = new RelayCommand(Back, CanBack);
        }

        public void Initialize(Frame frame) => this.frame = frame;
        public void Initialize(ISearchBox box) => this.box = box;

        public void Search()
        {
            if (box is not null)
            {
                if (string.IsNullOrWhiteSpace(box.Query))
                {
                    box.Query = "*";
                }
                box.Search();
            }
        }
        public void Back()
        {
            if (CanBack())
            {
                frame.GoBack(transition);
            }
        }
        private bool CanBack() => frame is not null && frame.CanGoBack;

        public void ClearPage() => GoPage((ISearchPageViewModel)null);

        public void GoPage(ISearchFilterViewModel filter)
        {
            if (filter is ISearchSettingsViewModel settings)
            {
                var viewModel = new SearchSettingsPageViewModel(settings);
                GoPage(viewModel);
            }
            else
            {
                var parentViewModel = (frame?.Content as SearchFilterPage)?.ViewModel;
                var childViewModel = viewModelFactory.GetPageViewModel(parentViewModel, filter);
                GoPage(childViewModel, transition);
            }
        }
        private void GoPage(ISearchPageViewModel viewModel, NavigationTransitionInfo transition = null)
        {
            if (frame is not null && viewModel != pageViewModel)
            {
                pageViewModel = viewModel;
                frame.Navigate(typeof(SearchFilterPage), pageViewModel, transition);
                OnPropertyChanged(nameof(PageViewModel));
                backCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
