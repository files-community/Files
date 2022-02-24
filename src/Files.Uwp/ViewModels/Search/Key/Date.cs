using Files.Enums;
using Files.Filesystem.Search;
using System.Collections.Generic;
using System.ComponentModel;

namespace Files.ViewModels.Search
{
    public interface IDateFilterViewModel : IMultiSearchFilterViewModel
    {
        DateRange Range { get; set; }
    }

    [SearchFilterViewModel(SearchKeys.DateCreated)]
    [SearchFilterViewModel(SearchKeys.DateModified)]
    [SearchFilterViewModel(SearchKeys.DateAccessed)]
    internal class DateFilterViewModel : MultiSearchFilterViewModel, IDateFilterViewModel
    {
        private readonly IDateFilter filter;

        public DateRange Range
        {
            get => filter.Range;
            set => filter.Range = value;
        }

        public DateFilterViewModel(IDateFilter filter) : base(filter)
        {
            this.filter = filter;
            filter.PropertyChanged += Filter_PropertyChanged;
        }

        private void Filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(Range))
            {
                OnPropertyChanged(nameof(Range));
            }
        }
    }

    [SearchPageViewModel(SearchKeys.DateCreated)]
    [SearchPageViewModel(SearchKeys.DateModified)]
    [SearchPageViewModel(SearchKeys.DateAccessed)]
    internal class DatePageViewModel : MultiSearchPageViewModel
    {
        public DatePageViewModel(ISearchPageViewModel parent, IDateFilterViewModel filter)
            : base(parent, filter) {}

        protected override IEnumerable<SearchKeys> GetKeys() => new List<SearchKeys>
        {
            SearchKeys.DateCreated,
            SearchKeys.DateModified,
            SearchKeys.DateAccessed,
        };
    }
}
