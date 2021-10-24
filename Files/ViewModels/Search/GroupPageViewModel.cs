using Files.Filesystem.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface IGroupPageViewModel : IMultiSearchPageViewModel
    {
        new IGroupPickerViewModel Picker { get; }
    }

    public interface IGroupPickerViewModel : IPickerViewModel
    {
        string Description { get; set; }
        IFilterCollection Filters { get; }
        IEnumerable<IFilterContext> Contexts { get; }

        ICommand OpenCommand { get; }
        ICommand ClearCommand { get; }
    }

    public interface IGroupHeader : IFilterHeader
    {
        IFilterCollection GetFilter(IEnumerable<IFilter> filters);
    }

    public interface IGroupContext : IFilterContext
    {
    }

    public class AndHeader : FilterHeader<AndFilterCollection>, IGroupHeader
    {
        IFilterCollection IGroupHeader.GetFilter(IEnumerable<IFilter> filters) => GetFilter(filters);
        public AndFilterCollection GetFilter(IEnumerable<IFilter> filters) => new(filters);
    }
    public class OrHeader : FilterHeader<OrFilterCollection>, IGroupHeader
    {
        IFilterCollection IGroupHeader.GetFilter(IEnumerable<IFilter> filters) => GetFilter(filters);
        public OrFilterCollection GetFilter(IEnumerable<IFilter> filters) => new(filters);
    }
    public class NotHeader : FilterHeader<NotFilterCollection>, IGroupHeader
    {
        IFilterCollection IGroupHeader.GetFilter(IEnumerable<IFilter> filters) => GetFilter(filters);
        public NotFilterCollection GetFilter(IEnumerable<IFilter> filters) => new(filters);
    }

    public class GroupContext : IGroupContext
    {
        private readonly ISearchPageContext context;
        private readonly IFilterCollection filter;

        public string Glyph => filter.Glyph;
        public string Label => filter.Title;
        public string Parameter => filter.Count switch
        {
            <= 1 => string.Format("({0} item)", filter.Count),
            _ => string.Format("({0} items)", filter.Count),
        };

        public ICommand OpenCommand { get; }
        public ICommand ClearCommand { get; }

        public GroupContext(ISearchPageContext context, IFilterCollection filter)
        {
            this.context = context;
            this.filter = filter;

            OpenCommand = new RelayCommand(Open);
            ClearCommand = new RelayCommand(Clear);
        }

        IFilter IFilterContext.GetFilter() => filter;
        public IFilterCollection GetFilter() => filter;

        private void Open() => context.GoPage(filter);
        private void Clear() => context.Save(null);
    }

    public class GroupPageViewModel : ObservableObject, IGroupPageViewModel
    {
        private readonly ISearchPageContext context;

        public IEnumerable<IFilterHeader> Headers { get; } = new List<IFilterHeader>
        {
            new AndHeader(),
            new OrHeader(),
            new NotHeader(),
        };

        private IFilterHeader header;
        public IFilterHeader Header
        {
            get => header;
            set
            {
                if (SetProperty(ref header, value))
                {
                    Picker.Description = header.Description;
                }
            }
        }

        IPickerViewModel ISearchPageViewModel.Picker => Picker;
        public IGroupPickerViewModel Picker { get; }

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AcceptCommand { get; }

        public GroupPageViewModel(ISearchPageContext context) : this(context, null)
        {
        }
        public GroupPageViewModel(ISearchPageContext context, IFilterCollection filter)
        {
            this.context = context;

            filter ??= new AndFilterCollection();

            header = filter switch
            {
                AndFilterCollection => Headers.First(h => h is AndHeader),
                OrFilterCollection => Headers.First(h => h is OrHeader),
                NotFilterCollection => Headers.First(h => h is NotHeader),
                _ => Headers.First(),
            };

            Picker = new GroupPickerViewModel(context, filter);
            Picker.Description = header?.Description;

            BackCommand = new RelayCommand(Back);
            SaveCommand = new RelayCommand(Save);
            AcceptCommand = new RelayCommand(Accept);
        }

        public void Back() => context.Back();
        public void Save()
        {
            if (Picker.IsEmpty)
            {
                context.Save(null);
            }
            else
            {
                var header = Header as IGroupHeader;
                var filter = header.GetFilter(Picker.Filters);
                context.Save(filter);
            }
        }
        public void Accept()
        {
            Save();
            Back();
        }
    }

    public class GroupPickerViewModel : ObservableObject, IGroupPickerViewModel
    {
        private readonly ISearchPageContext context;

        public bool IsEmpty => !Filters.Any();

        private string description;
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        public IFilterCollection Filters { get; }

        public IEnumerable<IFilterContext> Contexts
        {
            get
            {
                var factory = new FilterContextFactory(context);
                return Filters.Select(filter => factory.GetContext(filter));
            }
        }

        public ICommand ClearCommand { get; }
        public ICommand OpenCommand { get; }

        public GroupPickerViewModel(ISearchPageContext context, IFilterCollection filters) : base()
        {
            this.context = context;

            Filters = filters;
            Filters.PropertyChanged += Filters_PropertyChanged;

            ClearCommand = new RelayCommand(Clear);
            OpenCommand = new RelayCommand<IFilterHeader>(Open);
        }

        public void Clear() => Filters.Clear();

        private void Open(IFilterHeader header)
        {
            var filter = header.GetFilter();
            context.GoPage(filter);
        }

        private void Filters_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(Contexts));
        }

    }
}
