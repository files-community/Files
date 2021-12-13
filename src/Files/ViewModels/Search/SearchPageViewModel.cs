using Files.Filesystem.Search;
using Files.UserControls.Search;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface ISearchPageViewModel : INotifyPropertyChanged
    {
        ISearchPageNavigator Navigator { get; }
        ISearchFilterHeader Header { get; }
        IPickerViewModel Picker { get; }
    }

    public interface IMultiSearchPageViewModel : ISearchPageViewModel
    {
        IEnumerable<ISearchFilterHeader> Headers { get; }
        new ISearchFilterHeader Header { get; set; }
    }

    public interface ISearchPageNavigator
    {
        ICommand SearchCommand { get; }
        ICommand BackCommand { get; }

        void Search();
        void Back();
    }

    public interface ISearchPageContext : ISearchPageNavigator
    {
        void GoPage(ISearchFilter filter);
        void Save(ISearchFilter filter);

        ISearchPageContext GetChild(ISearchFilter filter);
    }

    public interface ISearchFilterHeader
    {
        string Glyph { get; }
        string Title { get; }
        string Description { get; }

        ISearchFilter GetFilter();
    }

    public interface ISearchFilterContext
    {
        string Glyph { get; }
        string Label { get; }
        string Parameter { get; }

        ICommand ClearCommand { get; }
        ICommand OpenCommand { get; }

        ISearchFilter GetFilter();
    }

    public interface IPickerViewModel : INotifyPropertyChanged
    {
        bool IsEmpty { get; }
        ICommand ClearCommand { get; }

        void Clear();
    }

    public interface ISearchPageViewModelFactory
    {
        ISearchPageViewModel GetViewModel(ISearchFilter filter);
    }

    public interface ISearchFilterContextFactory
    {
        ISearchFilterContext GetContext(ISearchFilter filter);
    }

    public class SearchFilterHeader<T> : ISearchFilterHeader where T : ISearchFilter, IHeader, new()
    {
        public string Glyph { get; }
        public string Title { get; }
        public string Description { get; }

        public SearchFilterHeader()
        {
            var filter = new T();
            Glyph = filter.Glyph;
            Title = filter.Title;
            Description = filter.Description;
        }

        ISearchFilter ISearchFilterHeader.GetFilter() => GetFilter();
        public T GetFilter() => new();
    }

    public class SearchPageViewModelFactory : ISearchPageViewModelFactory
    {
        private readonly ISearchPageContext context;

        public SearchPageViewModelFactory(ISearchPageContext context) => this.context = context;

        public ISearchPageViewModel GetViewModel(ISearchFilter filter) => filter switch
        {
            ISearchFilterCollection f => new GroupPageViewModel(context, f),
            IDateRangeFilter f => new DateRangePageViewModel(context, f),
            ISizeRangeFilter f => new SizeRangePageViewModel(context, f),
            _ => null,
        };
    }

    public class SearchFilterContextFactory : ISearchFilterContextFactory
    {
        private readonly ISearchPageContext parent;

        public SearchFilterContextFactory(ISearchPageContext parent) => this.parent = parent;

        public ISearchFilterContext GetContext(ISearchFilter filter) => filter switch
        {
            ISearchFilterCollection f => new GroupContext(parent, f),
            IDateRangeFilter f => new DateRangeContext(parent, f),
            ISizeRangeFilter f => new SizeRangeContext(parent, f),
            _ => null,
        };
    }

    public class SearchPageContext : ISearchPageContext
    {
        private readonly ISearchNavigator navigator;

        private readonly ISearchFilterCollection collection;
        private ISearchFilter filter;

        public ICommand SearchCommand { get; }
        public ICommand BackCommand { get; }

        private SearchPageContext()
        {
            SearchCommand = new RelayCommand(Search);
            BackCommand = new RelayCommand(Back);
        }
        public SearchPageContext(ISearchNavigator navigator, ISearchFilter filter) : this()
            => (this.navigator, this.filter) = (navigator, filter);
        private SearchPageContext(ISearchNavigator navigator, ISearchFilterCollection collection, ISearchFilter filter) : this(navigator, filter)
            => this.collection = collection;

        public void Search() => navigator?.Search();
        public void Back() => navigator?.Back();

        public void GoPage(ISearchFilter filter)
        {
            var child = GetChild(filter);
            var factory = new SearchPageViewModelFactory(child);
            var viewModel = factory.GetViewModel(filter);

            navigator.GoPage(viewModel);
        }

        public void Save(ISearchFilter filter)
        {
            if (collection is null)
            {
                return;
            }
            if (filter is null && collection.Contains(this.filter))
            {
                collection.Remove(this.filter);
            }
            else if (filter is not null)
            {
                if (collection.Contains(this.filter))
                {
                    int index = collection.IndexOf(this.filter);
                    collection[index] = filter;
                }
                else
                {
                    collection.Add(filter);
                }
            }
            this.filter = filter;
        }

        public ISearchPageContext GetChild(ISearchFilter filter)
            => new SearchPageContext(navigator, this.filter as ISearchFilterCollection, filter);
    }

    public abstract class SearchFilterContext<T> : ISearchFilterContext where T : ISearchFilter
    {
        private readonly ISearchPageContext parentPageContext;
        private readonly T filter;

        public virtual string Glyph => (filter as IHeader)?.Glyph;
        public virtual string Label => (filter as IHeader)?.Title;
        public virtual string Parameter => string.Empty;

        public ICommand ClearCommand { get; }
        public ICommand OpenCommand { get; }

        public SearchFilterContext(ISearchPageContext parentPageContext, T filter)
        {
            this.parentPageContext = parentPageContext;
            this.filter = filter;

            ClearCommand = new RelayCommand(Clear);
            OpenCommand = new RelayCommand(Open);
        }

        ISearchFilter ISearchFilterContext.GetFilter() => filter;
        public T GetFilter() => filter;

        private void Clear() => parentPageContext.GetChild(filter).Save(null);
        private void Open() => parentPageContext.GoPage(filter);
    }
}
