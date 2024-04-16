using Files.Backend.ViewModels.Dialogs.FileSystemDialog;
using Windows.UI.Xaml;

namespace Files.Uwp.TemplateSelectors
{
    internal sealed class FileSystemDialogItemSelector : BaseTemplateSelector<BaseFileSystemDialogItemViewModel>
    {
        public DataTemplate? ConflictItemDataTemplate { get; set; }

        public DataTemplate? DefaultItemDataTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(BaseFileSystemDialogItemViewModel? item, DependencyObject container)
        {
            if (item is FileSystemDialogConflictItemViewModel)
            {
                return ConflictItemDataTemplate!;
            }
            else if (item is FileSystemDialogDefaultItemViewModel)
            {
                return DefaultItemDataTemplate!;
            }
            else
            {
                return base.SelectTemplateCore(item, container);
            }
        }
    }
}
