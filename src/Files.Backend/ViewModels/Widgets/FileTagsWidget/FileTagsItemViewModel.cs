using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.Backend.Models;
using Files.Backend.ViewModels.FileTags;
using Files.Sdk.Storage.Extensions;
using Files.Sdk.Storage.LocatableStorage;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Backend.ViewModels.Widgets.FileTagsWidget
{
    public sealed partial class FileTagsItemViewModel : ObservableObject
    {
        private readonly ILocatableStorable _associatedStorable;

        [ObservableProperty]
        private FileTagViewModel _FileTag;

        [ObservableProperty]
        private IImageModel _Icon;

        [ObservableProperty]
        private string _Name;

        [ObservableProperty]
        private string? _Path;

        public FileTagsItemViewModel(ILocatableStorable associatedStorable, IImageModel icon, FileTagViewModel fileTag)
        {
            _associatedStorable = associatedStorable;
            _FileTag = fileTag;
            _Icon = icon;
            _Name = associatedStorable.Name;
            _Path = associatedStorable.TryGetPath();
        }

        [RelayCommand]
        private Task ClickAsync(CancellationToken cancellationToken)
        {
            _ = _associatedStorable;
            return Task.CompletedTask;
        }
    }
}
