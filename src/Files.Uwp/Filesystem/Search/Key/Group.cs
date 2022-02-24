using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Enums;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Files.Filesystem.Search
{
    public interface ISearchFilterCollection : ICollection<ISearchFilter>, IMultiSearchFilter, INotifyCollectionChanged
    {
    }

    [SearchHeader]
    internal class GroupAndHeader : ISearchHeader
    {
        public SearchKeys Key => SearchKeys.GroupAnd;

        public string Glyph => "\uEC26";
        public string Label => "And".GetLocalized();
        public string Description => "SearchAndFilterCollection_Description".GetLocalized();

        public ISearchFilter CreateFilter() => new SearchFilterCollection(Key);
    }

    [SearchHeader]
    internal class GroupOrHeader : ISearchHeader
    {
        public SearchKeys Key => SearchKeys.GroupOr;

        public string Glyph => "\uEC26";
        public string Label => "Or".GetLocalized();
        public string Description => "SearchOrFilterCollection_Description".GetLocalized();

        public ISearchFilter CreateFilter() => new SearchFilterCollection(Key);
    }

    [SearchHeader]
    internal class GroupNotHeader : ISearchHeader
    {
        public SearchKeys Key => SearchKeys.GroupNot;

        public string Glyph => "\uEC26";
        public string Label => "Not".GetLocalized();
        public string Description => "SearchNotFilterCollection_Description".GetLocalized();

        public ISearchFilter CreateFilter() => new SearchFilterCollection(Key);
    }

    internal class SearchFilterCollection : ObservableCollection<ISearchFilter>, ISearchFilterCollection
    {
        public SearchKeys Key
        {
            get => header.Key;
            set
            {

                if (value is not SearchKeys.GroupAnd and not SearchKeys.GroupOr and not SearchKeys.GroupNot)
                {
                    throw new ArgumentException();
                }
                if (header.Key != value)
                {
                    header = GetHeader(value);
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Header));
                }
            }
        }

        private ISearchHeader header;
        public ISearchHeader Header => header;

        public bool IsEmpty => !this.Any();

        public IEnumerable<ISearchTag> Tags => this.Any()
            ? new ISearchTag[1] { new Tag(this) }
            : Enumerable.Empty<ISearchTag>();

        public SearchFilterCollection(SearchKeys key) => header = GetHeader(key);
        public SearchFilterCollection(SearchKeys key, IList<ISearchFilter> filters) : base(filters) => header = GetHeader(key);

        public string ToAdvancedQuerySyntax()
        {
            var queries = this
                .Where(filter => filter is not null)
                .Select(filter => (filter.ToAdvancedQuerySyntax() ?? string.Empty).Trim())
                .Where(query => !string.IsNullOrEmpty(query));

            return Key switch
            {
                SearchKeys.GroupAnd => string.Join(' ', queries.Select(query => query.Contains(' ') ? $"({query})" : query)),
                SearchKeys.GroupOr => string.Join(" OR ", queries.Select(query => query.Contains(' ') ? $"({query})" : query)),
                SearchKeys.GroupNot => string.Join(' ', queries.Select(query => $"NOT({query})")),
                _ => throw new InvalidOperationException(),
            };
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            if (e.Action is NotifyCollectionChangedAction.Remove)
            {
                foreach (ISearchFilter filter in e.OldItems)
                {
                    filter.PropertyChanged -= Filter_PropertyChanged;
                }
            }
            if (e.Action is NotifyCollectionChangedAction.Add)
            {
                foreach (ISearchFilter filter in e.NewItems)
                {
                    filter.PropertyChanged += Filter_PropertyChanged;
                }
            }

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(Tags));
        }

        private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISearchFilter.IsEmpty))
            {
                if (sender is ISearchFilter filter && filter.IsEmpty)
                {
                    Remove(filter);
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        private static ISearchHeader GetHeader (SearchKeys key)
        {
            var provider = Ioc.Default.GetService<ISearchHeaderProvider>();
            return provider.GetHeader(key);
        }

        private class Tag : ISearchTag
        {
            ISearchFilter ISearchTag.Filter => Filter;
            public ISearchFilterCollection Filter { get; }

            public string Title => string.Empty;
            public string Parameter => GetParameter();

            public Tag(ISearchFilterCollection filter) => Filter = filter;

            public void Delete() => (Filter as ISearchFilter).Clear();

            private string GetParameter()
            {
                int count = Filter.Count;
                string labelKey = count <= 1 ? "GroupItemsCount_Singular" : "GroupItemsCount_Plural";

                return string.Format(labelKey.GetLocalized(), count);
            }
        }
    }
}
