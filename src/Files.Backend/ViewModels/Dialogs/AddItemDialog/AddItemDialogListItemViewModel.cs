using CommunityToolkit.Mvvm.ComponentModel;
using Files.Backend.Models.Dialogs;
using Files.Backend.Models.Imaging;

#nullable enable

namespace Files.Backend.ViewModels.Dialogs.AddItemDialog
{
    public sealed class AddItemDialogListItemViewModel : ObservableObject
    {
        public string? Header { get; set; }

        public string? SubHeader { get; set; }

        public string? Glyph { get; set; }

        public ImageModel? Icon { get; set; }

        public bool IsItemEnabled { get; set; }

        public AddItemDialogResultModel? ItemResult { get; set; }
    }
}
