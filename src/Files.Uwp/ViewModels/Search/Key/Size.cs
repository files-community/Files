using Files.Enums;
using Files.Filesystem.Search;
using System.ComponentModel;

namespace Files.ViewModels.Search
{
    public interface ISizeFilterViewModel : ISearchFilterViewModel
    {
        SizeRange Range { get; set; }
    }

    [SearchFilterViewModel(SearchKeys.Size)]
    internal class SizeFilterViewModel : SearchFilterViewModel, ISizeFilterViewModel
    {
        private readonly ISizeFilter filter;

        public SizeRange Range
        {
            get => filter.Range;
            set => filter.Range = value;
        }

        public SizeFilterViewModel(ISizeFilter filter) : base(filter)
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
}
