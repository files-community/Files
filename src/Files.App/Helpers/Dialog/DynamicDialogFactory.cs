// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Files.App.Helpers
{
	public static class DynamicDialogFactory
	{
		public static readonly SolidColorBrush _transparentBrush = new SolidColorBrush(Colors.Transparent);

		public static DynamicDialog GetFor_PropertySaveErrorDialog()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.PropertySaveErrorDialog_Title.GetLocalizedResource(),
				SubtitleText = Strings.PropertySaveErrorMessage_Text.GetLocalizedResource(), // We can use subtitle here as our content
				PrimaryButtonText = Strings.Retry.GetLocalizedResource(),
				SecondaryButtonText = Strings.PropertySaveErrorDialog_SecondaryButtonText.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary | DynamicDialogButtons.Cancel
			});
			return dialog;
		}

		public static DynamicDialog GetFor_ConsentDialog()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.WelcomeDialog_Title.GetLocalizedResource(),
				SubtitleText = Strings.WelcomeDialogTextBlock_Text.GetLocalizedResource(), // We can use subtitle here as our content
				PrimaryButtonText = Strings.WelcomeDialog_PrimaryButtonText.GetLocalizedResource(),
				PrimaryButtonAction = async (vm, e) => await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess")),
				DynamicButtons = DynamicDialogButtons.Primary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_ShortcutNotFound(string targetPath)
		{
			DynamicDialog dialog = new(new DynamicDialogViewModel
			{
				TitleText = Strings.ShortcutCannotBeOpened.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.DeleteShortcutDescription.GetLocalizedResource(), targetPath),
				PrimaryButtonText = Strings.Delete.GetLocalizedResource(),
				SecondaryButtonText = Strings.No.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_CreateItemDialog(string itemType)
		{
			DynamicDialog? dialog = null;
			TextBox inputText = new()
			{
				PlaceholderText = Strings.EnterAnItemName.GetLocalizedResource()
			};

			TeachingTip warning = new()
			{
				Title = Strings.InvalidFilename_Text.GetLocalizedResource(),
				PreferredPlacement = TeachingTipPlacementMode.Bottom,
				DataContext = new CreateItemDialogViewModel(),
			};

			warning.SetBinding(TeachingTip.TargetProperty, new Binding()
			{
				Source = inputText
			});
			warning.SetBinding(TeachingTip.IsOpenProperty, new Binding()
			{
				Mode = BindingMode.OneWay,
				Path = new PropertyPath("IsNameInvalid")
			});

			inputText.Resources.Add("InvalidNameWarningTip", warning);

			inputText.TextChanged += (textBox, args) =>
			{
				var isInputValid = FilesystemHelpers.IsValidForFilename(inputText.Text);
				((CreateItemDialogViewModel)warning.DataContext).IsNameInvalid = !string.IsNullOrEmpty(inputText.Text) && !isInputValid;
				dialog!.ViewModel.DynamicButtonsEnabled = isInputValid
														? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
														: DynamicDialogButtons.Cancel;
				if (isInputValid)
					dialog.ViewModel.AdditionalData = inputText.Text;
			};

			inputText.Loaded += (s, e) =>
			{
				// dispatching to the ui thread fixes an issue where the primary dialog button would steal focus
				_ = inputText.DispatcherQueue.EnqueueOrInvokeAsync(() => inputText.Focus(FocusState.Programmatic));
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = string.Format(Strings.CreateNewItemTitle.GetLocalizedResource(), itemType),
				SubtitleText = null,
				DisplayControl = new Grid()
				{
					MinWidth = 300d,
					Children =
					{
						inputText
					}
				},
				PrimaryButtonAction = (vm, e) =>
				{
					vm.HideDialog(); // Rename successful
				},
				PrimaryButtonText = Strings.Create.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});

			dialog.Closing += (s, e) =>
			{
				warning.IsOpen = false;
			};

			return dialog;
		}

		public static DynamicDialog GetFor_FileInUseDialog(List<Win32Process> lockingProcess = null)
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.FileInUseDialog_Title.GetLocalizedResource(),
				SubtitleText = lockingProcess.IsEmpty() ? Strings.FileInUseDialog_Text.GetLocalizedResource() :
					string.Format(Strings.FileInUseByDialog_Text.GetLocalizedResource(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})"))),
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
				PlaceholderText = Strings.CredentialDialogUserName_PlaceholderText.GetLocalizedResource()
			};

			PasswordBox inputPassword = new()
			{
				PlaceholderText = Strings.Password.GetLocalizedResource()
			};

			CheckBox saveCreds = new()
			{
				Content = Strings.NetworkAuthenticationSaveCheckbox.GetLocalizedResource()
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
				TitleText = Strings.NetworkAuthenticationDialogTitle.GetLocalizedResource(),
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.NetworkAuthenticationDialogMessage.GetLocalizedResource(), path.Substring(2)),
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

		public static DynamicDialog GetFor_GitCheckoutConflicts(string checkoutBranchName, string headBranchName)
		{
			DynamicDialog dialog = null!;

			var optionsListView = new ListView()
			{
				ItemsSource = new string[]
				{
					string.Format(Strings.BringChanges.GetLocalizedResource(), checkoutBranchName),
					string.Format(Strings.StashChanges.GetLocalizedResource(), headBranchName),
					Strings.DiscardChanges.GetLocalizedResource()
				},
				SelectionMode = ListViewSelectionMode.Single
			};
			optionsListView.SelectedIndex = 0;

			optionsListView.SelectionChanged += (listView, args) =>
			{
				dialog.ViewModel.AdditionalData = (GitCheckoutOptions)optionsListView.SelectedIndex;
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.SwitchBranch.GetLocalizedResource(),
				PrimaryButtonText = Strings.Switch.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				SubtitleText = Strings.UncommittedChanges.GetLocalizedResource(),
				DisplayControl = new Grid()
				{
					MinWidth = 250d,
					Children =
					{
						optionsListView
					}
				},
				AdditionalData = GitCheckoutOptions.BringChanges,
				CloseButtonAction = (vm, e) =>
				{
					dialog.ViewModel.AdditionalData = GitCheckoutOptions.None;
					vm.HideDialog();
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_GitHubConnectionError()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "Error".GetLocalizedResource(),
				SubtitleText = Strings.CannotReachGitHubError.GetLocalizedResource(),
				PrimaryButtonText = Strings.Close.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_GitCannotInitializeqRepositoryHere()
		{
			return new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "Error".GetLocalizedResource(),
				SubtitleText = Strings.CannotInitializeGitRepo.GetLocalizedResource(),
				PrimaryButtonText = Strings.Close.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});
		}

		public static DynamicDialog GetFor_DeleteGitBranchConfirmation(string branchName)
		{
			DynamicDialog dialog = null!;
			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.GitDeleteBranch.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.GitDeleteBranchSubtitle.GetLocalizedResource(), branchName),
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				AdditionalData = true,
				CloseButtonAction = (vm, e) =>
				{
					dialog.ViewModel.AdditionalData = false;
					vm.HideDialog();
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_RenameRequiresHigherPermissions(string path)
		{
			DynamicDialog dialog = null!;
			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.ItemRenameFailed.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.HigherPermissionsRequired.GetLocalizedResource(), path),
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				SecondaryButtonText = Strings.EditPermissions.GetLocalizedResource(),
				SecondaryButtonAction = (vm, e) =>
				{
					var context = Ioc.Default.GetRequiredService<IContentPageContext>();
					var item = context.ShellPage?.ShellViewModel.FilesAndFolders.FirstOrDefault(li => li.ItemPath.Equals(path));

					if (context.ShellPage is not null && item is not null)
						FilePropertiesHelpers.OpenPropertiesWindow(item, context.ShellPage, PropertiesNavigationViewItemType.Security);
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_CreateAlternateDataStreamDialog()
		{
			DynamicDialog? dialog = null;
			TextBox inputText = new()
			{
				PlaceholderText = Strings.EnterDataStreamName.GetLocalizedResource()
			};

			TeachingTip warning = new()
			{
				Title = Strings.InvalidFilename_Text.GetLocalizedResource(),
				PreferredPlacement = TeachingTipPlacementMode.Bottom,
				DataContext = new CreateItemDialogViewModel(),
			};

			warning.SetBinding(TeachingTip.TargetProperty, new Binding()
			{
				Source = inputText
			});
			warning.SetBinding(TeachingTip.IsOpenProperty, new Binding()
			{
				Mode = BindingMode.OneWay,
				Path = new PropertyPath("IsNameInvalid")
			});

			inputText.Resources.Add("InvalidNameWarningTip", warning);

			inputText.TextChanged += (textBox, args) =>
			{
				var isInputValid = FilesystemHelpers.IsValidForFilename(inputText.Text);
				((CreateItemDialogViewModel)warning.DataContext).IsNameInvalid = !string.IsNullOrEmpty(inputText.Text) && !isInputValid;
				dialog!.ViewModel.DynamicButtonsEnabled = isInputValid
														? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
														: DynamicDialogButtons.Cancel;
				if (isInputValid)
					dialog.ViewModel.AdditionalData = inputText.Text;
			};

			inputText.Loaded += (s, e) =>
			{
				// dispatching to the ui thread fixes an issue where the primary dialog button would steal focus
				_ = inputText.DispatcherQueue.EnqueueOrInvokeAsync(() => inputText.Focus(FocusState.Programmatic));
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = string.Format(Strings.CreateAlternateDataStream.GetLocalizedResource()),
				SubtitleText = null,
				DisplayControl = new Grid()
				{
					MinWidth = 300d,
					Children =
					{
						inputText
					}
				},
				PrimaryButtonAction = (vm, e) =>
				{
					vm.HideDialog();
				},
				PrimaryButtonText = Strings.Create.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});

			dialog.Closing += (s, e) =>
			{
				warning.IsOpen = false;
			};

			return dialog;
		}

		public static async Task ShowFor_IDEErrorDialog(string friendlyName)
		{
			var commands = Ioc.Default.GetRequiredService<ICommandManager>();
			var dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.IDENotLocatedTitle.GetLocalizedResource(),
				SubtitleText = string.Format(Strings.IDENotLocatedContent.GetLocalizedResource(), friendlyName),
				PrimaryButtonText = Strings.OpenSettings.GetLocalizedResource(),
				SecondaryButtonText = Strings.Close.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary,
			});

			await dialog.TryShowAsync();

			if (dialog.DynamicResult is DynamicDialogResult.Primary)
				await commands.OpenSettings.ExecuteAsync(
					new SettingsNavigationParams() { PageKind = SettingsPageKind.DevToolsPage }
				);
		}
		
		public static async Task ShowFor_CannotCloneRepo(string exception)
		{
			var dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = Strings.CannotCloneRepoTitle.GetLocalizedResource(),
				SubtitleText = exception,
				PrimaryButtonText = Strings.OK.GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});

			await dialog.TryShowAsync();
		}
	}
}
