namespace Files.ViewModels.Search
{
    public interface IFilterPageViewModel
    {
        IContainerFilterViewModel Parent { get; }
        IFilterViewModel Filter { get; }
    }

    public class FilterPageViewModel : IFilterPageViewModel
    {
        public IContainerFilterViewModel Parent { get; set; }
        public IFilterViewModel Filter { get; set; }
    }
}
