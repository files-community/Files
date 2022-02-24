using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.Filesystem.Search;
using Files.UserControls.Search;
using System.Windows.Input;

namespace Files.ViewModels.Search
{
    public interface ISearchTagViewModel
    {
        ISearchFilterViewModel Filter { get; }

        string Title { get; }
        string Parameter { get; }

        ICommand SelectCommand { get; }
        ICommand CloseCommand { get; }

        void Select(ISearchFilterViewModel middleFilter = null);
        void Close();
    }

    public class SearchTagViewModel : ISearchTagViewModel
    {
        private readonly ISearchTag tag;

        public ISearchFilterViewModel Filter { get; }

        public string Title => tag.Title;
        public string Parameter => tag.Parameter;

        public ICommand SelectCommand { get; }
        public ICommand CloseCommand { get; }

        public SearchTagViewModel(ISearchFilterViewModel filter, ISearchTag tag)
        {
            Filter = filter;
            this.tag = tag;

            SelectCommand = new RelayCommand<ISearchFilterViewModel>(Select);
            CloseCommand = new RelayCommand(Close);
        }

        public void Select(ISearchFilterViewModel middleFilter = null)
        {
            var navigator = Ioc.Default.GetService<ISearchNavigator>();

            if (middleFilter is not null)
            {
                navigator.GoPage(middleFilter);
            }

            navigator.GoPage(Filter);
        }

        public void Close() => tag.Delete();
    }
}
