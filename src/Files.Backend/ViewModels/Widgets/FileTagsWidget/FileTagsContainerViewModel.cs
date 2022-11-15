using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Models;
using Files.Shared.Utils;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.ViewModels.Widgets.FileTagsWidget
{
    public sealed partial class FileTagsContainerViewModel : ObservableObject, IAsyncInitialize
    {
        private readonly IFileTagsModel _fileTagsModel;

        public ObservableCollection<FileTagsItemViewModel> Tags { get; }

        public FileTagsContainerViewModel(IFileTagsModel fileTagsModel)
        {
            _fileTagsModel = fileTagsModel;
            Tags = new();
        }

        /// <inheritdoc/>
        public Task InitAsync(CancellationToken cancellationToken = default)
        {
            _ = _fileTagsModel;
            return Task.CompletedTask;
        }
    }
}
