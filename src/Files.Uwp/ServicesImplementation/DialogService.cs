using Files.Backend.ViewModels.Dialogs;
using Files.Backend.Services;
using Files.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Files.Uwp.ViewModels.Dialogs;
using Files.Uwp.Dialogs;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Files.Backend.ViewModels.Dialogs.FileSystemDialog;

namespace Files.Uwp.ServicesImplementation
{
    internal sealed class DialogService : IDialogService
    {
        private readonly Dictionary<Type, Func<ContentDialog>> _dialogs;

        public DialogService()
        {
            this._dialogs = new()
            {
                { typeof(AddItemDialogViewModel), () => new AddItemDialog() },
                { typeof(CredentialDialogViewModel), () => new CredentialDialog() },
                { typeof(ElevateConfirmDialogViewModel), () => new ElevateConfirmDialog() },
                { typeof(FileSystemDialogViewModel), () => new FilesystemOperationDialog() },
                { typeof(DecompressArchiveDialogViewModel), () => new DecompressArchiveDialog() },
                { typeof(SettingsDialogViewModel), () => new SettingsDialog() }
            };
        }

        public IDialog<TViewModel> GetDialog<TViewModel>(TViewModel viewModel)
            where TViewModel : class, INotifyPropertyChanged
        {
            _ = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            if (!_dialogs.TryGetValue(typeof(TViewModel), out var initializer))
            {
                throw new ArgumentException($"{typeof(TViewModel)} does not have a dialog associated with it.");
            }

            var contentDialog = initializer();

            if (contentDialog is not IDialog<TViewModel> dialog)
            {
                throw new NotSupportedException($"The dialog does not implement {typeof(IDialog<TViewModel>)}.");
            }

            dialog.ViewModel = viewModel;

            return dialog;
        }

        public async Task<DialogResult> ShowDialogAsync<TViewModel>(TViewModel viewModel)
            where TViewModel : class, INotifyPropertyChanged
        {
            try
            {
                return await GetDialog(viewModel).ShowAsync();
            }
            catch (Exception)
            {
            }

            return DialogResult.None;
        }
    }
}
