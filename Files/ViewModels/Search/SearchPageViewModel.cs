using Files.Filesystem.Search;
using Files.UserControls.Search;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface ISearchPageViewModel : INotifyPropertyChanged
    {
        IFilterHeader Header { get; }
        IPickerViewModel Picker { get; }

        ICommand BackCommand { get; }
        ICommand SaveCommand { get; }
        ICommand AcceptCommand { get; }
    }

    public interface IMultiSearchPageViewModel : ISearchPageViewModel
    {
        IEnumerable<IFilterHeader> Headers { get; }
        new IFilterHeader Header { get; set; }
    }

    public interface ISearchPageViewModelFactory
    {
        ISearchPageViewModel GetViewModel(IFilter filter);
    }

    public interface IPickerViewModel : INotifyPropertyChanged
    {
        bool IsEmpty { get; }
        ICommand ClearCommand { get; }
    }

    public interface IFilterHeader
    {
        string Glyph { get; }
        string Title { get; }
        string Description { get; }

        IFilter GetFilter();
    }

    public interface IFilterContext
    {
        string Glyph { get; }
        string Label { get; }
        string Parameter { get; }

        ICommand OpenCommand { get; }
        ICommand ClearCommand { get; }

        IFilter GetFilter();
    }

    public interface IFilterContextFactory
    {
        IFilterContext GetContext(IFilter filter);
    }

    public interface ISearchPageContext
    {
        ISearchPageContext GetChild(IFilter filter);

        void Back();
        void GoPage(IFilter filter);

        void Search();
        void Save(IFilter filter);
    }

    public class FilterHeader<T> : IFilterHeader where T : IFilter, IHeader, new()
    {
        public string Glyph { get; }
        public string Title { get; }
        public string Description { get; }

        public FilterHeader()
        {
            var filter = new T();
            Glyph = filter.Glyph;
            Title = filter.Title;
            Description = filter.Description;
        }

        IFilter IFilterHeader.GetFilter() => GetFilter();
        public T GetFilter() => new();
    }

    public class SearchPageViewModelFactory : ISearchPageViewModelFactory
    {
        private readonly ISearchPageContext context;

        public SearchPageViewModelFactory(ISearchPageContext context) => this.context = context;

        public ISearchPageViewModel GetViewModel(IFilter filter) => filter switch
        {
            IFilterCollection f => new GroupPageViewModel(context, f),
            IDateRangeFilter f => new DateRangePageViewModel(context, f),
            ISizeRangeFilter f => new SizeRangePageViewModel(context, f),
            _ => null,
        };
    }

    public class FilterContextFactory : IFilterContextFactory
    {
        private readonly ISearchPageContext groupContext;

        public FilterContextFactory(ISearchPageContext groupContext) => this.groupContext = groupContext;

        public IFilterContext GetContext(IFilter filter)
        {
            var childContext = groupContext.GetChild(filter);
            return filter switch
            {
                IFilterCollection f => new GroupContext(childContext, f),
                IDateRangeFilter f => new DateRangeContext(childContext, f),
                ISizeRangeFilter f => new SizeRangeContext(childContext, f),
                _ => null,
            };
        }
    }

    public class SearchPageContext : ISearchPageContext
    {
        private readonly INavigator navigator;

        private readonly IFilterCollection collection;
        private readonly IFilter filter;

        public SearchPageContext(INavigator navigator, IFilter filter)
            => (this.navigator, this.filter) = (navigator, filter);
        private SearchPageContext(INavigator navigator, IFilterCollection collection, IFilter filter) : this(navigator, filter)
            => this.collection = collection;

        ISearchPageContext ISearchPageContext.GetChild(IFilter filter) => GetChild(filter);
        public SearchPageContext GetChild(IFilter filter)
            => new SearchPageContext(navigator, this.filter as IFilterCollection, filter);

        public void Back() => navigator.GoBack();

        public void GoPage(IFilter filter)
        {
            var child = GetChild(filter);
            var factory = new SearchPageViewModelFactory(child);
            var viewModel = factory.GetViewModel(filter);

            navigator.GoPage(viewModel);
        }

        public void Search() {}

        public void Save(IFilter filter)
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
        }
    }
}
