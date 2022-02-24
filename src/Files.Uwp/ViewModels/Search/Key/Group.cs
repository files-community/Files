using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.Enums;
using Files.Extensions;
using Files.Filesystem.Search;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface ISearchFilterViewModelCollection : IReadOnlyCollection<ISearchFilterViewModel>, IMultiSearchFilterViewModel, INotifyCollectionChanged
    {
        new ISearchFilterCollection Filter { get; }

        string Description { get; }
    }

    [SearchFilterViewModel(SearchKeys.GroupAnd)]
    [SearchFilterViewModel(SearchKeys.GroupOr)]
    [SearchFilterViewModel(SearchKeys.GroupNot)]
    internal class SearchFilterViewModelCollection : ObservableCollection<ISearchFilterViewModel>, ISearchFilterViewModelCollection
    {
        private static readonly ISearchFilterViewModelFactory factory =
            Ioc.Default.GetService<ISearchFilterViewModelFactory>();

        ISearchFilter ISearchFilterViewModel.Filter => Filter;
        public ISearchFilterCollection Filter { get; }

        public SearchKeys Key
        {
            get => Filter.Key;
            set => Filter.Key = value;
        }

        private ISearchHeaderViewModel header;
        public ISearchHeaderViewModel Header => header;

        public virtual string Description => header.Description;

        public bool IsEmpty => Filter.IsEmpty;

        private IReadOnlyCollection<ISearchTagViewModel> tags;
        public IEnumerable<ISearchTagViewModel> Tags => tags;

        private readonly RelayCommand clearCommand;
        public ICommand ClearCommand => clearCommand;

        public SearchFilterViewModelCollection(ISearchFilterCollection filter)
        {
            Filter = filter;
            clearCommand = new RelayCommand((Filter as ISearchFilter).Clear, () => !Filter.IsEmpty);

            Filter.ForEach(f => Add(factory.GetFilterViewModel(f)));

            UpdateHeader();
            UpdateTags();

            filter.PropertyChanged += Filter_PropertyChanged;
            filter.CollectionChanged += Filter_CollectionChanged;
        }

        private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(IMultiSearchFilter.Key):
                    OnPropertyChanged(nameof(Key));
                    break;
                case nameof(ISearchFilter.Header):
                    UpdateHeader();
                    OnPropertyChanged(nameof(Header));
                    break;
                case nameof(ISearchFilter.IsEmpty):
                    OnPropertyChanged(nameof(IsEmpty));
                    clearCommand.NotifyCanExecuteChanged();
                    break;
                case nameof(ISearchFilter.Tags):
                    UpdateTags();
                    OnPropertyChanged(nameof(Tags));
                    break;
            }
        }
        private void Filter_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    Clear();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    var replacedItem = e.NewItems[e.OldStartingIndex] as ISearchFilter;
                    this[e.NewStartingIndex] = factory.GetFilterViewModel(replacedItem);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var _ in e.OldItems)
                    {
                        RemoveAt(e.OldStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    var newItems = e.NewItems.Cast<ISearchFilter>().Reverse();
                    foreach (var newItem in newItems)
                    {
                        var vm = factory.GetFilterViewModel(newItem);
                        Insert(e.NewStartingIndex, vm);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    MoveItem(e.OldStartingIndex, e.NewStartingIndex);
                    break;
            }
        }

        private void UpdateHeader()
            => header = new SearchHeaderViewModel(Filter.Header);
        private void UpdateTags()
            => tags = Filter.Tags.Select(tag => new SearchTagViewModel(this, tag)).ToList().AsReadOnly();

        private void OnPropertyChanged(string propertyName)
            => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    [SearchPageViewModel(SearchKeys.GroupAnd)]
    [SearchPageViewModel(SearchKeys.GroupOr)]
    [SearchPageViewModel(SearchKeys.GroupNot)]
    internal class GroupPageViewModel : MultiSearchPageViewModel
    {
        public GroupPageViewModel(ISearchPageViewModel parent, ISearchFilterViewModelCollection filter)
            : base(parent, filter) {}

        protected override IEnumerable<SearchKeys> GetKeys() => new List<SearchKeys>
        {
            SearchKeys.GroupAnd,
            SearchKeys.GroupOr,
            SearchKeys.GroupNot,
        };
    }
}
