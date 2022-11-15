using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Models;
using Files.Backend.ViewModels.FileTags;
using Files.Sdk.Storage;
using Files.Sdk.Storage.Extensions;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.ViewModels.Widgets.FileTagsWidget
{
    public sealed partial class FileTagsItemViewModel : ObservableObject
    {
        private readonly IStorable _associatedTagStorable;

        [ObservableProperty]
        private FileTagViewModel _FileTag;

        [ObservableProperty]
        private IImageModel _Icon;

        [ObservableProperty]
        private string _Name;

        [ObservableProperty]
        private string? _Path;

        public FileTagsItemViewModel(IStorable associatedTagStorable, IImageModel icon, FileTagViewModel fileTag)
        {
            _associatedTagStorable = associatedTagStorable;
            _FileTag = fileTag;
            _Icon = icon;
            _Name = associatedTagStorable.Name;
            _Path = associatedTagStorable.TryGetPath();
        }

        [RelayCommand]
        private Task ClickAsync(CancellationToken cancellationToken)
        {
            _ = _associatedTagStorable;
            return Task.CompletedTask;
        }
    }
}
