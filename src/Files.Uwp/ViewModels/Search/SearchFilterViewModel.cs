using Files.Enums;
using Files.Filesystem.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Files.ViewModels.Search
{
    public interface ISearchFilterViewModel : ISearchContentViewModel
    {
        ISearchFilter Filter { get; }

        ISearchHeaderViewModel Header { get; }

        IEnumerable<ISearchTagViewModel> Tags { get; }
    }

    public interface IMultiSearchFilterViewModel : ISearchFilterViewModel
    {
        SearchKeys Key { get; set; }
    }

    public interface ISearchFilterViewModelFactory
    {
        ISearchFilterViewModel GetFilterViewModel(ISearchFilter filter);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class SearchFilterViewModelAttribute : Attribute
    {
        public SearchKeys Key { get; set; } = SearchKeys.None;

        public SearchFilterViewModelAttribute() {}
        public SearchFilterViewModelAttribute(SearchKeys key) => Key = key;
    }

    internal class SearchFilterViewModel : SearchContentViewModel, ISearchFilterViewModel
    {
        public ISearchFilter Filter { get; }

        private ISearchHeaderViewModel header;
        public ISearchHeaderViewModel Header => header;

        private IReadOnlyCollection<ISearchTagViewModel> tags;
        public IEnumerable<ISearchTagViewModel> Tags => tags;

        public SearchFilterViewModel(ISearchFilter filter) : base(filter)
        {
            Filter = filter;
            UpdateHeader();
            UpdateTags();

            filter.PropertyChanged += Filter_PropertyChanged;
        }

        private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ISearchFilter.Header):
                    UpdateHeader();
                    OnPropertyChanged(nameof(Header));
                    break;
                case nameof(ISearchFilter.Tags):
                    UpdateTags();
                    OnPropertyChanged(nameof(Tags));
                    break;
            }
        }

        private void UpdateHeader()
            => header = new SearchHeaderViewModel(Filter.Header);
        private void UpdateTags()
            => tags = Filter.Tags.Select(tag => new SearchTagViewModel(this, tag)).ToList().AsReadOnly();
    }

    internal class MultiSearchFilterViewModel : SearchFilterViewModel, IMultiSearchFilterViewModel
    {
        private readonly IMultiSearchFilter filter;

        public SearchKeys Key
        {
            get => filter.Key;
            set => filter.Key = value;
        }

        public MultiSearchFilterViewModel(IMultiSearchFilter filter) : base(filter)
        {
            this.filter = filter;

            filter.PropertyChanged += Filter_PropertyChanged;
        }

        private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(IMultiSearchFilter.Key))
            {
                OnPropertyChanged(nameof(Key));
            }
        }
    }

    internal class SearchFilterViewModelFactory : ISearchFilterViewModelFactory
    {
        private readonly IReadOnlyDictionary<SearchKeys, Factory> factories = GetFactories();

        public ISearchFilterViewModel GetFilterViewModel(ISearchFilter filter) => filter switch
        {
            ISearchFilter when factories.ContainsKey(filter.Header.Key) => factories[filter.Header.Key].Build(filter),
            ISearchFilter => new SearchFilterViewModel(filter),
            _ => null,
        };

        private static IReadOnlyDictionary<SearchKeys, Factory> GetFactories()
        {
            var factories = new Dictionary<SearchKeys, Factory>();

            var assembly = Assembly.GetExecutingAssembly();
            foreach (Type type in assembly.GetTypes())
            {
                var attributes = type.GetCustomAttributes(typeof(SearchFilterViewModelAttribute), false).Cast<SearchFilterViewModelAttribute>();
                foreach (var attribute in attributes)
                {
                    factories[attribute.Key] = new Factory(type);
                }
            }

            return new ReadOnlyDictionary<SearchKeys, Factory>(factories);
        }

        private class Factory
        {
            private readonly Type type;

            public Factory(Type type) => this.type = type;

            public ISearchFilterViewModel Build(ISearchFilter filter)
                => Activator.CreateInstance(type, new object[] { filter }) as ISearchFilterViewModel;
        }
    }
}
