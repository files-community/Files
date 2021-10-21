using Files.Filesystem.Search;
using Files.UserControls.Search;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    /*public interface IGroupPageViewModel : IFilterPageViewModel, INotifyPropertyChanged
    {
        FilterCollection Filters { get; }
        ICommand OpenCommand { get; }
    }

    public class AndSource : IFilterSource
    {
        public string Key => "and";
        public string Glyph => "\uEC26";
        public string Title => "And";
        public string Description => "Finds items that meet all the conditions in the list.";
    }
    public class OrSource : IFilterSource
    {
        public string Key => "or";
        public string Glyph => "\uEC26";
        public string Title => "Or";
        public string Description => "Finds items that meet at least one condition in the list.";
    }
    public class NotSource : IFilterSource
    {
        public string Key => "not";
        public string Glyph => "\uEC26";
        public string Title => "Not";
        public string Description => "Finds items that do not meet any condition in the list.";
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

        public ICommand OpenCommand { get; }

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
            OpenCommand = new RelayCommand<string>(Open);
        }

        public override void Clear() => Filters.Clear();

        protected override IFilterLink CreateLink() => new FilterLink(this);

        private void Open (string sourceKey)
        {
            IFilter filter = sourceKey switch
            {
                "and" => new AndFilterCollection(),
                "or" => new OrFilterCollection(),
                "not" => new NotFilterCollection(),
                "created" => new CreatedFilter(DateRange.Always),
                "modified" => new ModifiedFilter(DateRange.Always),
                "accessed" => new AccessedFilter(DateRange.Always),
                _ => null,
            };

            var factory = new FilterPageViewModelFactory();
            var viewModel = factory.GetViewModel(Filters, filter);

            Navigator.Instance.GoPage(viewModel);
        }

        private class FilterLink : IFilterLink
        {
            public IFilterSource Source { get; set; }

            IFilter IFilterLink.Filter => throw new NotImplementedException();
            public FilterCollection Filter { get; set; }

            public string Text => $"{Filter.Count} items";

            public FilterLink(IGroupPageViewModel viewModel)
            {
                Source = viewModel.SelectedSource;
                Filter = viewModel.SelectedSource.Key switch
                {
                    "and" => new AndFilterCollection(viewModel.Filters),
                    "or" => new OrFilterCollection(viewModel.Filters),
                    "not" => new NotFilterCollection(viewModel.Filters),
                    _ => throw new ArgumentException(),
                };
            }
        }
    }*/
}
