using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.Filesystem.Search;
using System.ComponentModel;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface ISearchContentViewModel : INotifyPropertyChanged
    {
        bool IsEmpty { get; }

        ICommand ClearCommand { get; }

        void Clear();
    }

    internal class SearchContentViewModel : ObservableObject, ISearchContentViewModel
    {
        private readonly ISearchContent content;

        public bool IsEmpty => content.IsEmpty;

        private readonly RelayCommand clearCommand;
        public ICommand ClearCommand => clearCommand;

        public SearchContentViewModel(ISearchContent content)
        {
            this.content = content;
            clearCommand = new RelayCommand(content.Clear, () => !content.IsEmpty);

            content.PropertyChanged += Content_PropertyChanged;
        }

        public void Clear() => content.Clear();

        private void Content_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ISearchContent.IsEmpty))
            {
                OnPropertyChanged(nameof(IsEmpty));
                clearCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
