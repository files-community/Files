using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Dialogs;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.ViewModels;
using Files.App.ViewModels.Dialogs;
using Files.Shared.Enums;
using Files.Shared.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.System;

namespace Files.App.Helpers
{
	public static class DynamicDialogFactory
	{
		public static DynamicDialog GetFor_PropertySaveErrorDialog()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "PropertySaveErrorDialog/Title".GetLocalizedResource(),
				SubtitleText = "PropertySaveErrorMessage/Text".GetLocalizedResource(), // We can use subtitle here as our content
				PrimaryButtonText = "Retry".GetLocalizedResource(),
				SecondaryButtonText = "PropertySaveErrorDialog/SecondaryButtonText".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary | DynamicDialogButtons.Cancel
			});
			return dialog;
		}

		public static DynamicDialog GetFor_ConsentDialog()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "WelcomeDialog/Title".GetLocalizedResource(),
				SubtitleText = "WelcomeDialogTextBlock/Text".GetLocalizedResource(), // We can use subtitle here as our content
				PrimaryButtonText = "WelcomeDialog/PrimaryButtonText".GetLocalizedResource(),
				PrimaryButtonAction = async (vm, e) => await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess")),
				DynamicButtons = DynamicDialogButtons.Primary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_ShortcutNotFound(string targetPath)
		{
			DynamicDialog dialog = new(new DynamicDialogViewModel
			{
				TitleText = "ShortcutCannotBeOpened".GetLocalizedResource(),
				SubtitleText = string.Format("DeleteShortcutDescription".GetLocalizedResource(), targetPath),
				PrimaryButtonText = "Delete".GetLocalizedResource(),
				SecondaryButtonText = "No".GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_RenameDialog()
		{
			DynamicDialog? dialog = null;
			TextBox inputText = new()
			{
				PlaceholderText = "EnterAnItemName".GetLocalizedResource()
			};

			TextBlock tipText = new()
			{
				Text = "InvalidFilename/Text".GetLocalizedResource(),
				Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 4, 0),
				TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
				Opacity = 0.0d
			};

			inputText.TextChanged += (textBox, args) =>
			{
				var isInputValid = FilesystemHelpers.IsValidForFilename(inputText.Text);
				tipText.Opacity = isInputValid ? 0.0d : 1.0d;
				dialog!.ViewModel.DynamicButtonsEnabled = isInputValid
														? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
														: DynamicDialogButtons.Cancel;
				if (isInputValid)
					dialog.ViewModel.AdditionalData = inputText.Text;
			};

			inputText.Loaded += (s, e) =>
			{
				// dispatching to the ui thread fixes an issue where the primary dialog button would steal focus
				_ = inputText.DispatcherQueue.EnqueueAsync(() => inputText.Focus(Microsoft.UI.Xaml.FocusState.Programmatic));
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "EnterAnItemName".GetLocalizedResource(),
				SubtitleText = null,
				DisplayControl = new Grid()
				{
					MinWidth = 300d,
					Children =
					{
						new StackPanel()
						{
							Spacing = 4d,
							Children =
							{
								inputText,
								tipText
							}
						}
					}
				},
				PrimaryButtonAction = (vm, e) =>
				{
					vm.HideDialog(); // Rename successful
				},
				PrimaryButtonText = "RenameDialog/PrimaryButtonText".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});

			return dialog;
		}

		public static DynamicDialog GetFor_FileInUseDialog(List<Shared.Win32Process> lockingProcess = null)
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "FileInUseDialog/Title".GetLocalizedResource(),
				SubtitleText = lockingProcess.IsEmpty() ? "FileInUseDialog/Text".GetLocalizedResource() :
					string.Format("FileInUseByDialog/Text".GetLocalizedResource(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})"))),
				PrimaryButtonText = "OK",
				DynamicButtons = DynamicDialogButtons.Primary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_CredentialEntryDialog(string path)
		{
			string[] userAndPass = new string[3];
			DynamicDialog? dialog = null;

			TextBox inputUsername = new()
			{
				PlaceholderText = "CredentialDialogUserName/PlaceholderText".GetLocalizedResource()
			};

			PasswordBox inputPassword = new()
			{
				PlaceholderText = "Password".GetLocalizedResource()
			};

			CheckBox saveCreds = new()
			{
				Content = "NetworkAuthenticationSaveCheckbox".GetLocalizedResource()
			};

			inputUsername.TextChanged += (textBox, args) =>
			{
				userAndPass[0] = inputUsername.Text;
				dialog.ViewModel.AdditionalData = userAndPass;
			};

			inputPassword.PasswordChanged += (textBox, args) =>
			{
				userAndPass[1] = inputPassword.Password;
				dialog.ViewModel.AdditionalData = userAndPass;
			};

			saveCreds.Checked += (textBox, args) =>
			{
				userAndPass[2] = "y";
				dialog.ViewModel.AdditionalData = userAndPass;
			};

			saveCreds.Unchecked += (textBox, args) =>
			{
				userAndPass[2] = "n";
				dialog.ViewModel.AdditionalData = userAndPass;
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "NetworkAuthenticationDialogTitle".GetLocalizedResource(),
				PrimaryButtonText = "OK".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				SubtitleText = string.Format("NetworkAuthenticationDialogMessage".GetLocalizedResource(), path.Substring(2)),
				DisplayControl = new Grid()
				{
					MinWidth = 250d,
					Children =
					{
						new StackPanel()
						{
							Spacing = 10d,
							Children =
							{
								inputUsername,
								inputPassword,
								saveCreds
							}
						}
					}
				},
				CloseButtonAction = (vm, e) =>
				{
					dialog.ViewModel.AdditionalData = null;
					vm.HideDialog();
				}

			});

			return dialog;
		}
	}
}