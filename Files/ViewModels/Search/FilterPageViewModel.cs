using Files.Filesystem.Search;
using Files.UserControls.Search;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface IFilterPageViewModel
    {
        IEnumerable<IFilterSource> Sources { get; }
        IFilterSource SelectedSource { get; set; }

        bool IsEmpty { get; }

        ICommand ClearCommand { get; }
        ICommand BackCommand { get; }
        ICommand SaveCommand { get; }
        ICommand AcceptCommand { get; }

        void Clear();
        void Back();
        void Save();
        void Accept();
    }

    public interface IFilterSource
    {
        string Key { get; }
        string Glyph { get; }
        string Title { get; }
        string Description { get; }
    }

    public interface IFilterPageViewModelFactory
    {
        IFilterPageViewModel GetViewModel(FilterCollection parent, IFilter filter);
    }

    public abstract class FilterPageViewModel : ObservableObject, IFilterPageViewModel
    {
        private readonly Navigator navigator = Navigator.Instance;

        public FilterCollection Parent { get; }
        public IFilter Filter { get; }

        public abstract IEnumerable<IFilterSource> Sources { get; }

        private IFilterSource selectedSource;
        public IFilterSource SelectedSource
        {
            get => selectedSource;
            set
            {
                if (!Sources.Contains(value))
                {
                    throw new ArgumentException();
                }
                SetProperty(ref selectedSource, value);
            }
        }

        public abstract bool IsEmpty { get; }

        public ICommand ClearCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand AcceptCommand { get; }

        public FilterPageViewModel(FilterCollection parent, IFilter filter)
        {
            Parent = parent;
            Filter = filter;
            SelectedSource = Sources.FirstOrDefault();

            ClearCommand = new RelayCommand(Clear);
            BackCommand = new RelayCommand(Back);
            SaveCommand = new RelayCommand(Save);
            AcceptCommand = new RelayCommand(Accept);
        }

        public virtual void Clear() {}
        public virtual void Back() => navigator.GoBack();

        public virtual void Save()
        {
            if (Parent is null)
            {
                return;
            }
            if (Parent.Contains(Filter))
            {
                if (IsEmpty)
                {
                    Parent.Remove(Filter);
                }
                else
                {
                    int index = Parent.IndexOf(Filter);
                    Parent[index] = CreateFilter();
                }
            }
            else if (!IsEmpty)
            {
                Parent.Add(CreateFilter());
            }
        }
        public virtual void Accept()
        {
            Save();
            Back();
        }

        protected abstract IFilter CreateFilter();
    }

    public class FilterSource : IFilterSource
    {
        public string Key { get; set; }
        public string Glyph { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class FilterPageViewModelFactory : IFilterPageViewModelFactory
    {
        public IFilterPageViewModel GetViewModel(FilterCollection parent, IFilter filter) => filter switch
        {
            AndFilterCollection f => new GroupPageViewModel(parent, f),
            OrFilterCollection f => new GroupPageViewModel(parent, f),
            NotFilterCollection f => new GroupPageViewModel(parent, f),
            CreatedFilter f => new DateRangePageViewModel(parent, f),
            ModifiedFilter f => new DateRangePageViewModel(parent, f),
            AccessedFilter f => new DateRangePageViewModel(parent, f),
            _ => null,
        };
    }
}
