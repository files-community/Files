using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Enums;
using Files.Extensions;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Files.Filesystem.Search
{
    public interface ISearchSettings : ISearchContent
    {
        bool SearchInSubFolders { get; set; }

        ISearchFilterCollection Filter { get; }
    }

    public class SearchSettings : ObservableObject, ISearchSettings
    {
        private readonly int pinnedCount;

        public bool searchInSubFolders = true;
        public bool SearchInSubFolders
        {
            get => searchInSubFolders;
            set
            {
                if (SetProperty(ref searchInSubFolders, value))
                {
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool IsEmpty => searchInSubFolders
            && Filter.Count == pinnedCount && Filter.Take(pinnedCount).All(filter => filter.IsEmpty);

        public ISearchFilterCollection Filter { get; }

        public SearchSettings()
        {
            var pinnedKeys = new SearchKeys[] { SearchKeys.Size, SearchKeys.DateModified };
            pinnedCount = pinnedKeys.Length;

            var provider = Ioc.Default.GetService<ISearchHeaderProvider>();
            var pinneds = pinnedKeys.Select(key => GetFilter(key)).ToList();

            Filter = new SearchFilterCollection(SearchKeys.GroupAnd, pinneds);
            Filter.CollectionChanged += Filter_CollectionChanged;
            Filter.Take(pinnedCount).ForEach(filter => filter.PropertyChanged += PinnedFilter_PropertyChanged);

            ISearchFilter GetFilter(SearchKeys key) => provider.GetHeader(key).CreateFilter();
        }

        public void Clear()
        {
            SearchInSubFolders = true;

            Filter.Take(pinnedCount).ForEach(subFilter => subFilter.Clear());
            Filter.Skip(pinnedCount).ToList().ForEach(subFilter => Filter.Remove(subFilter));
        }

        private void Filter_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsEmpty));
        }
        private void PinnedFilter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISearchFilter.IsEmpty))
            {
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }
}
