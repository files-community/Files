using Files.Filesystem.Search;
using Files.UserControls.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface IFilterPageViewModel : INotifyPropertyChanged
    {
        IFilterHeader Header { get; }
        IPickerViewModel Picker { get; }

        ICommand BackCommand { get; }
        ICommand SaveCommand { get; }
        ICommand AcceptCommand { get; }
    }

    public interface IMultiFilterPageViewModel : IFilterPageViewModel
    {
        IEnumerable<IFilterHeader> Headers { get; }
        new IFilterHeader Header { get; set; }
    }

    public interface IPickerViewModel : INotifyPropertyChanged
    {
        bool IsEmpty { get; }
        ICommand ClearCommand { get; }
    }

    public interface IFilterHeader
    {
        string Glyph { get; }
        string Title { get; }
        string Description { get; }

        IFilter GetFilter();
    }

    public abstract class FilterPageViewModel : ObservableObject, IFilterPageViewModel
    {
        private readonly Navigator navigator = Navigator.Instance;

        public IFilterHeader Header { get; }
        public IPickerViewModel Picker { get; }

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AcceptCommand { get; }

        public FilterPageViewModel()
        {
            Picker = GetPicker();

            BackCommand = new RelayCommand(Back);
            SaveCommand = new RelayCommand(Save);
            AcceptCommand = new RelayCommand(Accept);
        }

        public virtual void Back() => navigator.GoBack();
        public virtual void Save() {}
        public virtual void Accept() { Save(); Back(); }

        protected abstract IFilterHeader GetHeader();
        protected abstract IPickerViewModel GetPicker();
    }

    public abstract class MultiFilterPageViewModel : ObservableObject, IMultiFilterPageViewModel
    {
        private readonly Navigator navigator = Navigator.Instance;

        public abstract IEnumerable<IFilterHeader> Headers { get; }

        private IFilterHeader header;
        public IFilterHeader Header
        {
            get => header;
            set
            {
                if (SetProperty(ref header, value))
                {
                    picker = GetPicker();
                    OnPropertyChanged(nameof(Picker));
                }
            }
        }

        private IPickerViewModel picker;
        public IPickerViewModel Picker => picker;

        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AcceptCommand { get; }

        public MultiFilterPageViewModel()
        {
            Header = Headers.First();
            picker = GetPicker();

            BackCommand = new RelayCommand(Back);
            SaveCommand = new RelayCommand(Save);
            AcceptCommand = new RelayCommand(Accept);
        }

        public virtual void Back() => navigator.GoBack();
        public virtual void Save() { }
        public virtual void Accept() { Save(); Back(); }

        protected abstract IEnumerable<IFilterHeader> GetHeaders();
        protected abstract IPickerViewModel GetPicker();
    }
}
