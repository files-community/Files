using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Windows.Input;
using Windows.Foundation;

namespace Files.ViewModels.Search
{
    public interface INavigatorViewModel : INotifyPropertyChanged
    {
        event TypedEventHandler<INavigatorViewModel, PageOpenedNavigatorEventArgs> PageOpened;
        event TypedEventHandler<INavigatorViewModel, EventArgs> BackRequested;
        event TypedEventHandler<INavigatorViewModel, EventArgs> ForwardRequested;
        event TypedEventHandler<INavigatorViewModel, EventArgs> SearchRequested;

        ICommand BackCommand { get; }
        ICommand ForwardCommand { get; }
        ICommand SearchCommand { get; }

        void OpenPage(object viewModel);
        void Back();
        void Forward();
        void Search();
    }

    public class PageOpenedNavigatorEventArgs : EventArgs
    {
        public object ViewModel { get; }

        public PageOpenedNavigatorEventArgs(object viewModel) => ViewModel = viewModel;
    }

    public class NavigatorViewModel : ObservableObject, INavigatorViewModel
    {
        public static NavigatorViewModel Default = new();

        public event TypedEventHandler<INavigatorViewModel, PageOpenedNavigatorEventArgs> PageOpened;
        public event TypedEventHandler<INavigatorViewModel, EventArgs> BackRequested;
        public event TypedEventHandler<INavigatorViewModel, EventArgs> ForwardRequested;
        public event TypedEventHandler<INavigatorViewModel, EventArgs> SearchRequested;

        public ICommand BackCommand { get; }
        public ICommand ForwardCommand { get; }
        public ICommand SearchCommand { get; }

        private NavigatorViewModel()
        {
            BackCommand = new RelayCommand(Back);
            ForwardCommand = new RelayCommand(Forward);
            SearchCommand = new RelayCommand(Search);
        }

        public void OpenPage(object viewModel)
            => PageOpened?.Invoke(this, new PageOpenedNavigatorEventArgs(viewModel));

        public void Search() => SearchRequested?.Invoke(this, EventArgs.Empty);
        public void Back() => BackRequested?.Invoke(this, EventArgs.Empty);
        public void Forward() => ForwardRequested?.Invoke(this, EventArgs.Empty);
    }
}
