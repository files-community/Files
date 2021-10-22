using Files.Filesystem.Search;
using Files.UserControls.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface IGroupPageViewModel : IMultiFilterPageViewModel
    {
        new IGroupPickerViewModel Picker { get; }
    }

    public interface IGroupPickerViewModel : IPickerViewModel
    {
        string Description { get; set; }
        ObservableCollection<IFilter> Filters { get; }
        ICommand OpenCommand { get; }
    }

    public interface IGroupHeader : IFilterHeader
    {
        FilterCollection GetFilter(IEnumerable<IFilter> filters);
    }

    public class AndHeader : FilterHeader<AndFilterCollection>, IGroupHeader
    {
        FilterCollection IGroupHeader.GetFilter(IEnumerable<IFilter> filters) => GetFilter(filters);
        public AndFilterCollection GetFilter(IEnumerable<IFilter> filters) => new(filters);
    }
    public class OrHeader : FilterHeader<OrFilterCollection>, IGroupHeader
    {
        FilterCollection IGroupHeader.GetFilter(IEnumerable<IFilter> filters) => GetFilter(filters);
        public OrFilterCollection GetFilter(IEnumerable<IFilter> filters) => new(filters);
    }
    public class NotHeader : FilterHeader<NotFilterCollection>, IGroupHeader
    {
        FilterCollection IGroupHeader.GetFilter(IEnumerable<IFilter> filters) => GetFilter(filters);
        public NotFilterCollection GetFilter(IEnumerable<IFilter> filters) => new(filters);
    }

    public class GroupPageViewModel : ObservableObject, IGroupPageViewModel
    {
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

        IPickerViewModel IFilterPageViewModel.Picker => Picker;
        public IGroupPickerViewModel Picker { get; } = new GroupPickerViewModel();

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AcceptCommand { get; }

        public GroupPageViewModel()
        {
            BackCommand = new RelayCommand(Back);
            SaveCommand = new RelayCommand(Save);
            AcceptCommand = new RelayCommand(Accept);
        }
        public GroupPageViewModel(FilterCollection filter) : this()
        {
            header = filter switch
            {
                AndFilterCollection => Headers.First(h => h is AndHeader),
                OrFilterCollection => Headers.First(h => h is OrHeader),
                NotFilterCollection => Headers.First(h => h is NotHeader),
                _ => Headers.First(),
            };
            if (filter is not null)
            {
                Picker = new GroupPickerViewModel(filter);
            }
            Picker.Description = header?.Description;
        }

        public void Back()
        {
            Navigator.Instance.GoBack();
        }
        public void Save()
        {
            var collection = Navigator.Instance.CurrentCollection;
            if (collection is not null)
            {
                if (!Picker.IsEmpty)
                {
                    var header = Header as IGroupHeader;
                    collection.Add(header.GetFilter(Picker.Filters));
                }
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
        public bool IsEmpty => !Filters.Any();

        private string description;
        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        public ObservableCollection<IFilter> Filters { get; } = new ObservableCollection<IFilter>();

        public ICommand ClearCommand { get; }
        public ICommand OpenCommand { get; }

        public GroupPickerViewModel() : this(Enumerable.Empty<IFilter>())
        {
        }
        public GroupPickerViewModel(IEnumerable<IFilter> filters) : base()
        {
            Filters = new ObservableCollection<IFilter>(filters);

            ClearCommand = new RelayCommand(Clear);
            OpenCommand = new RelayCommand<IFilterHeader>(Open);
        }

        public void Clear() => Filters.Clear();

        private void Open(IFilterHeader header)
        {
            var filter = header.GetFilter();
            var factory = new FilterPageViewModelFactory();
            var viewModel = factory.GetViewModel(filter);

            Navigator.Instance.GoPage(viewModel);
        }
    }
}
