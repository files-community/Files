using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Models;
using Files.Backend.Services;
using Files.Shared.Utils;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.ViewModels.Widgets.FileTagsWidget
{
    public sealed partial class FileTagsContainerViewModel : ObservableObject, IAsyncInitialize
    {
        private IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();

        public ObservableCollection<FileTagsItemViewModel> Tags { get; }

        [ObservableProperty]
        private IColorModel? _TagColor;

        [ObservableProperty]
        private string _TagName;

        public FileTagsContainerViewModel()
        {
            Tags = new();
        }

        /// <inheritdoc/>
        public Task InitAsync(CancellationToken cancellationToken = default)
        {
            //FileTagsService.GetAllFileTagsAsync();
            return Task.CompletedTask;
        }
    }
}
