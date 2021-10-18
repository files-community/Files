using Files.Filesystem.Search;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Files.ViewModels.Search
{
    public interface IGroupPageViewModel : IFilterPageViewModel, INotifyPropertyChanged
    {
        FilterCollection Filters { get; }
    }

    public class AndSource : IFilterSource
    {
        public string Key => "and";
        public string Glyph => "\uEC26";
        public string Title => "Group AND";
        public string Description => "Finds files and folders that meet all the conditions in the list.";
    }
    public class OrSource : IFilterSource
    {
        public string Key => "or";
        public string Glyph => "\uEC26";
        public string Title => "Group OR";
        public string Description => "Finds files and folders that meet at least one condition in the list.";
    }
    public class NotSource : IFilterSource
    {
        public string Key => "not";
        public string Glyph => "\uEC26";
        public string Title => "Group NOT";
        public string Description => "Finds files and folders that do not meet any condition in the list.";
    }

    public class GroupPageViewModel : FilterPageViewModel, IGroupPageViewModel
    {
        public override IEnumerable<IFilterSource> Sources { get; } = new List<IFilterSource>
        {
            new AndSource(),
            new OrSource(),
            new NotSource(),
        };

        public override bool IsEmpty => !Filters.Any();

        public FilterCollection Filters { get; }

        public GroupPageViewModel(FilterCollection parent, FilterCollection filter) : base(parent, filter)
        {
            SelectedSource = filter switch
            {
                AndFilterCollection => Sources.First(source => source.Key == "and"),
                OrFilterCollection => Sources.First(source => source.Key == "or"),
                NotFilterCollection => Sources.First(source => source.Key == "not"),
                _ => SelectedSource,
            };


            Filters = filter;
            Filters.CollectionChanged += Filters_CollectionChanged;
        }

        public override void Clear() => Filters.Clear();

        protected override IFilter CreateFilter() => SelectedSource.Key switch
        {
            "and" => new AndFilterCollection(Filters),
            "or" => new OrFilterCollection(Filters),
            "not" => new NotFilterCollection(Filters),
            _ => null,
        };

        private void Filters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => OnPropertyChanged(nameof(IsEmpty));
    }
}
