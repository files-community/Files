using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Models;
using Files.Shared.Utils;

namespace Files.Backend.ViewModels.Widgets.FileTagsWidget
{
    public sealed partial class FileTagsWidgetViewModel : ObservableObject, IAsyncInitialize
    {
        private readonly IFileTagsModel _fileTagsModel;

        public ObservableCollection<FileTagsContainerViewModel> Containers { get; }

        public FileTagsWidgetViewModel(IFileTagsModel fileTagsModel)
        {
            _fileTagsModel = fileTagsModel;
            Containers = new();
        }

        /// <inheritdoc/>
        public Task InitAsync(CancellationToken cancellationToken = default)
        {
            _ = _fileTagsModel;
            return Task.CompletedTask;
        }
    }
}
