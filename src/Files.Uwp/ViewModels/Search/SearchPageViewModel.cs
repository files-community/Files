using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
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
    public interface ISearchPageViewModel : INotifyPropertyChanged
    {
        ISearchPageViewModel Parent { get; }
        ISearchFilterViewModel Filter { get; }
    }

    public interface IMultiSearchPageViewModel : ISearchPageViewModel
    {
        SearchKeys Key { get; set; }
        IEnumerable<ISearchHeaderViewModel> Headers { get; }

        new IMultiSearchFilterViewModel Filter { get; }
    }

    public interface ISearchPageViewModelFactory
    {
        ISearchPageViewModel GetPageViewModel(ISearchPageViewModel parent, ISearchFilterViewModel filter);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class SearchPageViewModelAttribute : Attribute
    {
        public SearchKeys Key { get; set; } = SearchKeys.None;

        public SearchPageViewModelAttribute() {}
        public SearchPageViewModelAttribute(SearchKeys key) => Key = key;
    }

    internal class SearchPageViewModel : ObservableObject, ISearchPageViewModel
    {
        public ISearchPageViewModel Parent { get; }

        public ISearchFilterViewModel Filter { get; }

        public SearchPageViewModel(ISearchPageViewModel parent, ISearchFilterViewModel filter)
            => (Parent, Filter) = (parent, filter);
    }

    internal abstract class MultiSearchPageViewModel : ObservableObject, IMultiSearchPageViewModel
    {
        public ISearchPageViewModel Parent { get; }

        public SearchKeys Key
        {
            get => Filter.Header.Key;
            set
            {
                if (Filter.Header.Key != value)
                {
                    (Filter as IMultiSearchFilterViewModel).Key = value;
                    OnPropertyChanged(nameof(Key));
                }
            }
        }

        public IEnumerable<ISearchHeaderViewModel> Headers { get; }

        ISearchFilterViewModel ISearchPageViewModel.Filter => Filter;
        public IMultiSearchFilterViewModel Filter { get; }

        public MultiSearchPageViewModel(ISearchPageViewModel parent, IMultiSearchFilterViewModel filter)
        {
            (Parent, Filter) = (parent, filter);

            var provider = Ioc.Default.GetService<ISearchHeaderProvider>();
            Headers = GetKeys().Select(key => new SearchHeaderViewModel(provider.GetHeader(key))).ToList();
        }

        protected abstract IEnumerable<SearchKeys> GetKeys();
    }

    internal class SearchPageViewModelFactory : ISearchPageViewModelFactory
    {
        private readonly IReadOnlyDictionary<SearchKeys, Factory> factories = GetFactories();

        public ISearchPageViewModel GetPageViewModel(ISearchPageViewModel parent, ISearchFilterViewModel filter) => filter switch
        {
            ISearchSettingsViewModel settings => new SearchSettingsPageViewModel(settings),
            _ when factories.ContainsKey(filter.Header.Key) => factories[filter.Header.Key].Build(parent, filter),
            _ => new SearchPageViewModel(parent, filter),
        };

        private static IReadOnlyDictionary<SearchKeys, Factory> GetFactories()
        {
            var factories = new Dictionary<SearchKeys, Factory>();

            var assembly = Assembly.GetExecutingAssembly();
            foreach (Type type in assembly.GetTypes())
            {
                var attributes = type.GetCustomAttributes(typeof(SearchPageViewModelAttribute), false).Cast<SearchPageViewModelAttribute>();
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

            public ISearchPageViewModel Build(ISearchPageViewModel parent, ISearchFilterViewModel filter)
                => Activator.CreateInstance(type, new object[] { parent, filter }) as ISearchPageViewModel;
        }
    }

    public static class SearchPageViewModelExtensions
    {
        public static void Save(this ISearchPageViewModel pageViewModel)
        {
            var filter = pageViewModel?.Filter;
            if (!filter.IsEmpty)
            {
                var parent = pageViewModel?.Parent?.Filter;
                if (parent is ISearchSettingsViewModel settings)
                {
                    Save(settings.Collection);
                }
                if (parent is ISearchFilterViewModelCollection collection)
                {
                    Save(collection);
                }
            }

            void Save(ISearchFilterViewModel parent)
            {
                if (parent is ISearchFilterViewModelCollection collection && !collection.Filter.Contains(filter.Filter))
                {
                    collection.Filter.Add(filter.Filter);
                    pageViewModel?.Parent?.Save();
                }
            }
        }
    }
}
