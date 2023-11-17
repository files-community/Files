// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.IO;
using System.Windows.Input;

namespace Files.App.ViewModels.UserControls
{
	/// <summary>
	/// Represents ViewModel for <see cref="Files.App.UserControls.AddressToolbar"/>.
	/// </summary>
	public class AddressToolbarViewModel : ObservableObject, IAddressToolbar, IDisposable
	{
		// Dependency injection

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private IDialogService DialogService { get; } = Ioc.Default.GetRequiredService<IDialogService>();
		private IUpdateService UpdateService { get; } = Ioc.Default.GetRequiredService<IUpdateService>();
		private ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		// Event handlers

		public delegate void AddressBarTextEnteredEventHandler(object sender, AddressBarTextEnteredEventArgs e);
		public event IAddressToolbar.ToolbarQuerySubmittedEventHandler? PathBoxQuerySubmitted;
		public event AddressBarTextEnteredEventHandler? AddressBarTextEntered;
		public event EventHandler? EditModeEnabled;
		public event EventHandler? RefreshRequested;
		public event EventHandler? RefreshWidgetsRequested;

		// Observable properties

		private bool _IsCommandPaletteOpen;
		public bool IsCommandPaletteOpen
		{
			get => _IsCommandPaletteOpen;
			set => SetProperty(ref _IsCommandPaletteOpen, value);
		}

		private bool _IsUpdating;
		public bool IsUpdating
		{
			get => _IsUpdating;
			set => SetProperty(ref _IsUpdating, value);
		}

		private bool _IsUpdateAvailable;
		public bool IsUpdateAvailable
		{
			get => _IsUpdateAvailable;
			set => SetProperty(ref _IsUpdateAvailable, value);
		}

		private string? _ReleaseNotes;
		public string? ReleaseNotes
		{
			get => _ReleaseNotes;
			set => SetProperty(ref _ReleaseNotes, value);
		}

		private bool _IsReleaseNotesVisible;
		public bool IsReleaseNotesVisible
		{
			get => _IsReleaseNotesVisible;
			set => SetProperty(ref _IsReleaseNotesVisible, value);
		}

		private bool _CanCopyPathInPage;
		public bool CanCopyPathInPage
		{
			get => _CanCopyPathInPage;
			set => SetProperty(ref _CanCopyPathInPage, value);
		}

		private bool _CanGoBack;
		public bool CanGoBack
		{
			get => _CanGoBack;
			set => SetProperty(ref _CanGoBack, value);
		}

		private bool _CanGoForward;
		public bool CanGoForward
		{
			get => _CanGoForward;
			set => SetProperty(ref _CanGoForward, value);
		}

		private bool _CanNavigateToParent;
		public bool CanNavigateToParent
		{
			get => _CanNavigateToParent;
			set => SetProperty(ref _CanNavigateToParent, value);
		}

		private bool _IsPreviewPaneEnabled;
		public bool IsPreviewPaneEnabled
		{
			get => _IsPreviewPaneEnabled;
			set => SetProperty(ref _IsPreviewPaneEnabled, value);
		}

		private bool _CanRefresh;
		public bool CanRefresh
		{
			get => _CanRefresh;
			set => SetProperty(ref _CanRefresh, value);
		}

		private string _SearchButtonGlyph = "\uE721";
		public string SearchButtonGlyph
		{
			get => _SearchButtonGlyph;
			set => SetProperty(ref _SearchButtonGlyph, value);
		}

		private bool _IsSearchBoxVisible;
		public bool IsSearchBoxVisible
		{
			get => _IsSearchBoxVisible;
			set
			{
				if (SetProperty(ref _IsSearchBoxVisible, value))
					SearchButtonGlyph = value ? "\uE711" : "\uE721";
			}
		}

		private string? _PathText;
		public string? PathText
		{
			get => _PathText;
			set => SetProperty(ref _PathText, value);
		}

		private CurrentInstanceViewModel? _InstanceViewModel;
		public CurrentInstanceViewModel InstanceViewModel
		{
			get => _InstanceViewModel;
			set
			{
				if (_InstanceViewModel?.FolderSettings is not null)
					_InstanceViewModel.FolderSettings.PropertyChanged -= FolderSettings_PropertyChanged;

				if (SetProperty(ref _InstanceViewModel, value) && _InstanceViewModel?.FolderSettings is not null)
				{
					FolderSettings_PropertyChanged(this, new PropertyChangedEventArgs(nameof(FolderSettingsViewModel.LayoutMode)));
					_InstanceViewModel.FolderSettings.PropertyChanged += FolderSettings_PropertyChanged;
				}
			}
		}

		private Style? _LayoutOpacityIcon;
		public Style LayoutOpacityIcon
		{
			get => _LayoutOpacityIcon;
			set => SetProperty(ref _LayoutOpacityIcon, value);
		}

		private bool _HasItem = false;
		public bool HasItem
		{
			get => _HasItem;
			set => SetProperty(ref _HasItem, value);
		}

		private List<ListedItem>? _SelectedItems;
		public List<ListedItem> SelectedItems
		{
			get => _SelectedItems;
			set
			{
				if (SetProperty(ref _SelectedItems, value))
				{
					OnPropertyChanged(nameof(CanCopy));
					OnPropertyChanged(nameof(CanExtract));
					OnPropertyChanged(nameof(ExtractToText));
					OnPropertyChanged(nameof(IsArchiveOpened));
					OnPropertyChanged(nameof(IsSelectionArchivesOnly));
					OnPropertyChanged(nameof(IsMultipleArchivesSelected));
					OnPropertyChanged(nameof(IsInfFile));
					OnPropertyChanged(nameof(IsPowerShellScript));
					OnPropertyChanged(nameof(IsImage));
					OnPropertyChanged(nameof(IsMultipleImageSelected));
					OnPropertyChanged(nameof(IsFont));
					OnPropertyChanged(nameof(HasAdditionalAction));
				}
			}
		}

		private ISearchBox _SearchBox = new SearchBoxViewModel();
		public ISearchBox SearchBox
		{
			get => _SearchBox;
			set => SetProperty(ref _SearchBox, value);
		}

		private bool _ManualEntryBoxLoaded;
		public bool ManualEntryBoxLoaded
		{
			get => _ManualEntryBoxLoaded;
			set => SetProperty(ref _ManualEntryBoxLoaded, value);
		}

		private bool _ClickablePathLoaded = true;
		public bool ClickablePathLoaded
		{
			get => _ClickablePathLoaded;
			set => SetProperty(ref _ClickablePathLoaded, value);
		}

		private string? _PathControlDisplayText;
		public string PathControlDisplayText
		{
			get => _PathControlDisplayText;
			set => SetProperty(ref _PathControlDisplayText, value);
		}

		/// <summary>
		/// Used to hide the selection, sort, & layout toolbar buttons on the home page
		/// </summary>
		public bool _IsRightToolbarVisible = false;
		public bool IsRightToolbarVisible
		{
			get => _IsRightToolbarVisible;
			set => SetProperty(ref _IsRightToolbarVisible, value);
		}

		// Auto properties

		public ObservableCollection<NavigationBarSuggestionItem> NavigationBarSuggestions { get; } = new();

		public bool IsEditModeEnabled
		{
			get => ManualEntryBoxLoaded;
			set
			{
				if (value)
				{
					EditModeEnabled?.Invoke(this, EventArgs.Empty);

					var visiblePath = AddressToolbar?.FindDescendant<AutoSuggestBox>(x => x.Name == "VisiblePath");
					visiblePath?.Focus(FocusState.Programmatic);
					visiblePath?.FindDescendant<TextBox>()?.SelectAll();

					AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = visiblePath });
				}
				else
				{
					IsCommandPaletteOpen = false;
					ManualEntryBoxLoaded = false;
					ClickablePathLoaded = true;
				}
			}
		}

		public SearchBoxViewModel SearchBoxViewModel
			=> (SearchBoxViewModel)SearchBox;

		public ICommand? OpenNewWindowCommand { get; }
		public ICommand? CreateNewFileCommand { get; }
		public ICommand? Share { get; }
		public ICommand? UpdateCommand { get; }
		public ICommand? RefreshClickCommand { get; }
		public ICommand? ViewReleaseNotesAsyncCommand { get; }

		public bool SearchHasFocus { get; private set; }

		public bool HasAdditionalAction => InstanceViewModel.IsPageTypeRecycleBin || IsPowerShellScript || CanExtract || IsImage || IsFont || IsInfFile;
		public bool CanCopy => SelectedItems is not null && SelectedItems.Any();
		public bool CanExtract => IsArchiveOpened ? (SelectedItems is null || !SelectedItems.Any()) : IsSelectionArchivesOnly;
		public bool IsArchiveOpened => FileExtensionHelpers.IsZipFile(Path.GetExtension(_PathControlDisplayText));
		public bool IsSelectionArchivesOnly => SelectedItems is not null && SelectedItems.Any() && SelectedItems.All(x => FileExtensionHelpers.IsZipFile(x.FileExtension)) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsMultipleArchivesSelected => IsSelectionArchivesOnly && SelectedItems.Count > 1;
		public bool IsPowerShellScript => SelectedItems is not null && SelectedItems.Count == 1 && FileExtensionHelpers.IsPowerShellFile(SelectedItems.First().FileExtension) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsImage => SelectedItems is not null && SelectedItems.Any() && SelectedItems.All(x => FileExtensionHelpers.IsImageFile(x.FileExtension)) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsMultipleImageSelected => SelectedItems is not null && SelectedItems.Count > 1 && SelectedItems.All(x => FileExtensionHelpers.IsImageFile(x.FileExtension)) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsInfFile => SelectedItems is not null && SelectedItems.Count == 1 && FileExtensionHelpers.IsInfFile(SelectedItems.First().FileExtension) && !InstanceViewModel.IsPageTypeRecycleBin;
		public bool IsFont => SelectedItems is not null && SelectedItems.Any() && SelectedItems.All(x => FileExtensionHelpers.IsFontFile(x.FileExtension)) && !InstanceViewModel.IsPageTypeRecycleBin;

		public bool IsTilesLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.TilesView;
		public bool IsColumnLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.ColumnView;
		public bool IsGridSmallLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.GridView && _InstanceViewModel.FolderSettings.GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeSmall;
		public bool IsGridMediumLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.GridView && !IsGridSmallLayout && _InstanceViewModel.FolderSettings.GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeMedium;
		public bool IsGridLargeLayout => _InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes.GridView && !IsGridSmallLayout && !IsGridMediumLayout;
		public bool IsDetailsLayout => !IsTilesLayout && !IsColumnLayout && !IsGridSmallLayout && !IsGridMediumLayout && !IsGridLargeLayout;

		public string ExtractToText =>
			IsSelectionArchivesOnly
				? SelectedItems.Count > 1
					? string.Format("ExtractToChildFolder".GetLocalizedResource(), $"*{Path.DirectorySeparatorChar}")
					: string.Format("ExtractToChildFolder".GetLocalizedResource() + "\\", Path.GetFileNameWithoutExtension(_SelectedItems.First().Name))
				: "ExtractToChildFolder".GetLocalizedResource();

		private static AddressToolbar? AddressToolbar
			=> (MainWindow.Instance.Content as Frame)?.FindDescendant<AddressToolbar>();

		// Methods

		public AddressToolbarViewModel()
		{
			ViewReleaseNotesAsyncCommand = new AsyncRelayCommand(ViewReleaseNotesAsync);
			CreateNewFileCommand = new AsyncRelayCommand<ShellNewEntry>(x => UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(AddItemDialogItemType.File, x, ContentPageContext.ShellPage));
			OpenNewWindowCommand = new AsyncRelayCommand(NavigationHelpers.LaunchNewWindowAsync);
			RefreshClickCommand = new RelayCommand<RoutedEventArgs>(e => RefreshRequested?.Invoke(this, EventArgs.Empty));
			UpdateCommand = new AsyncRelayCommand(UpdateService.DownloadUpdatesAsync);

			IsRightToolbarVisible = ContentPageContext.PageType is not ContentPageTypes.Home;

			SearchBox.Escaped += SearchRegion_Escaped;
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
			UpdateService.PropertyChanged += UpdateService_OnPropertyChanged;
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsRightToolbarVisible));
					break;
			}
		}

		private async void UpdateService_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			IsUpdateAvailable = UpdateService.IsUpdateAvailable;
			IsUpdating = UpdateService.IsUpdating;

			// TODO: Bad code, result is called twice when checking for release notes
			if (UpdateService.IsReleaseNotesAvailable)
				await CheckForReleaseNotesAsync();
		}

		private async Task ViewReleaseNotesAsync()
		{
			if (ReleaseNotes is null)
				return;

			var viewModel = new ReleaseNotesDialogViewModel(ReleaseNotes);
			var dialog = DialogService.GetDialog(viewModel);

			await dialog.TryShowAsync();
		}

		private async Task CheckForReleaseNotesAsync()
		{
			var result = await UpdateService.GetLatestReleaseNotesAsync();
			if (result is null)
				return;

			ReleaseNotes = result;
			IsReleaseNotesVisible = true;
		}

		private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				// TODO: Move this to the widget page, it doesn't belong here.
				case nameof(UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowDrivesWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowFileTagsWidget):
				case nameof(UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget):
					RefreshWidgetsRequested?.Invoke(this, EventArgs.Empty);
					OnPropertyChanged(e.SettingName);
					break;
			}
		}

		public void VisiblePath_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
				AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = sender });
		}

		public void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			PathBoxQuerySubmitted?.Invoke(this, new ToolbarQuerySubmittedEventArgs() { QueryText = args.QueryText });

			(this as IAddressToolbar).IsEditModeEnabled = false;
		}

		public void OpenCommandPalette()
		{
			PathText = ">";
			IsCommandPaletteOpen = true;
			ManualEntryBoxLoaded = true;
			ClickablePathLoaded = false;

			var visiblePath = AddressToolbar?.FindDescendant<AutoSuggestBox>(x => x.Name == "VisiblePath");
			AddressBarTextEntered?.Invoke(this, new AddressBarTextEnteredEventArgs() { AddressBarTextField = visiblePath });
		}

		public void SwitchSearchBoxVisibility()
		{
			if (IsSearchBoxVisible)
			{
				CloseSearchBox(true);
			}
			else
			{
				IsSearchBoxVisible = true;

				// Given that binding and layouting might take a few cycles, when calling UpdateLayout
				// we can guarantee that the focus call will be able to find an open ASB
				var searchbox = AddressToolbar?.FindDescendant("SearchRegion") as SearchBox;
				searchbox?.UpdateLayout();
				searchbox?.Focus(FocusState.Programmatic);
			}
		}

		public void UpdateAdditionalActions()
		{
			OnPropertyChanged(nameof(HasAdditionalAction));
		}

		private void CloseSearchBox(bool doFocus = false)
		{
			if (_SearchBox.WasQuerySubmitted)
			{
				_SearchBox.WasQuerySubmitted = false;
			}
			else
			{
				SearchBox.Query = string.Empty;
				IsSearchBoxVisible = false;

				if (doFocus)
				{
					var page = Ioc.Default.GetRequiredService<IContentPageContext>().ShellPage?.SlimContentPage;

					if (page is StandardViewBase svb && svb.IsLoaded)
						page.ItemManipulationModel.FocusFileList();
					else
						AddressToolbar?.Focus(FocusState.Programmatic);
				}
			}
		}

		public void SearchRegion_GotFocus(object sender, RoutedEventArgs e)
		{
			SearchHasFocus = true;
		}

		public void SearchRegion_LostFocus(object sender, RoutedEventArgs e)
		{
			var element = FocusManager.GetFocusedElement();
			if (element is FlyoutBase or AppBarButton)
				return;

			SearchHasFocus = false;
			CloseSearchBox();
		}

		private void SearchRegion_Escaped(object? sender, ISearchBox searchBox)
			=> CloseSearchBox(true);

		public async Task CheckPathInputAsync(string currentInput, string currentSelectedPath, IShellPage shellPage)
		{
			if (currentInput.StartsWith('>'))
			{
				var code = currentInput.Substring(1).Trim();
				var command = Commands[code];

				if (command == Commands.None)
					await DialogDisplayHelper.ShowDialogAsync("InvalidCommand".GetLocalizedResource(),
						string.Format("InvalidCommandContent".GetLocalizedResource(), code));
				else if (!command.IsExecutable)
					await DialogDisplayHelper.ShowDialogAsync("CommandNotExecutable".GetLocalizedResource(),
						string.Format("CommandNotExecutableContent".GetLocalizedResource(), command.Code));
				else
					await command.ExecuteAsync();

				return;
			}

			var isFtp = FtpHelpers.IsFtpPath(currentInput);

			if (currentInput.Contains('/') && !isFtp)
				currentInput = currentInput.Replace("/", "\\", StringComparison.Ordinal);

			currentInput = currentInput.Replace("\\\\", "\\", StringComparison.Ordinal);

			if (currentInput.StartsWith('\\') && !currentInput.StartsWith("\\\\", StringComparison.Ordinal))
				currentInput = currentInput.Insert(0, "\\");

			if (currentSelectedPath == currentInput || string.IsNullOrWhiteSpace(currentInput))
				return;

			if (currentInput != shellPage.FilesystemViewModel.WorkingDirectory || shellPage.CurrentPageType == typeof(HomePage))
			{
				if (currentInput.Equals("Home", StringComparison.OrdinalIgnoreCase) || currentInput.Equals("Home".GetLocalizedResource(), StringComparison.OrdinalIgnoreCase))
				{
					shellPage.NavigateHome();
				}
				else
				{
					currentInput = StorageFileExtensions.GetResolvedPath(currentInput, isFtp);
					if (currentSelectedPath == currentInput)
						return;

					var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(currentInput));

					var resFolder = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(currentInput, item));
					if (resFolder || FolderHelpers.CheckFolderAccessWithWin32(currentInput))
					{
						var matchingDrive = DrivesViewModel.Drives.Cast<DriveItem>().FirstOrDefault(x => PathNormalization.NormalizePath(currentInput).StartsWith(PathNormalization.NormalizePath(x.Path), StringComparison.Ordinal));
						if (matchingDrive is not null && matchingDrive.Type == Data.Items.DriveType.CDRom && matchingDrive.MaxSpace == ByteSizeLib.ByteSize.FromBytes(0))
						{
							bool ejectButton = await DialogDisplayHelper.ShowDialogAsync("InsertDiscDialog/Title".GetLocalizedResource(), string.Format("InsertDiscDialog/Text".GetLocalizedResource(), matchingDrive.Path), "InsertDiscDialog/OpenDriveButton".GetLocalizedResource(), "Close".GetLocalizedResource());
							if (ejectButton)
							{
								var result = await DriveHelpers.EjectDeviceAsync(matchingDrive.Path);
								await UIHelpers.ShowDeviceEjectResultAsync(matchingDrive.Type, result);
							}
							return;
						}
						var pathToNavigate = resFolder.Result?.Path ?? currentInput;
						shellPage.NavigateToPath(pathToNavigate);
					}
					else if (isFtp)
					{
						shellPage.NavigateToPath(currentInput);
					}
					else // Not a folder or inaccessible
					{
						var resFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileWithPathFromPathAsync(currentInput, item));
						if (resFile)
						{
							var pathToInvoke = resFile.Result.Path;
							await Win32Helpers.InvokeWin32ComponentAsync(pathToInvoke, shellPage);
						}
						else // Not a file or not accessible
						{
							var workingDir =
								string.IsNullOrEmpty(shellPage.FilesystemViewModel.WorkingDirectory) ||
								shellPage.CurrentPageType == typeof(HomePage) ?
									Constants.UserEnvironmentPaths.HomePath :
									shellPage.FilesystemViewModel.WorkingDirectory;

							if (await LaunchApplicationFromPath(currentInput, workingDir))
								return;

							try
							{
								if (!await Windows.System.Launcher.LaunchUriAsync(new Uri(currentInput)))
									await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalizedResource(),
										string.Format("InvalidItemDialogContent".GetLocalizedResource(), Environment.NewLine, resFolder.ErrorCode.ToString()));
							}
							catch (Exception ex) when (ex is UriFormatException || ex is ArgumentException)
							{
								await DialogDisplayHelper.ShowDialogAsync("InvalidItemDialogTitle".GetLocalizedResource(),
									string.Format("InvalidItemDialogContent".GetLocalizedResource(), Environment.NewLine, resFolder.ErrorCode.ToString()));
							}
						}
					}
				}

				PathControlDisplayText = shellPage.FilesystemViewModel.WorkingDirectory;
			}
		}

		private static async Task<bool> LaunchApplicationFromPath(string currentInput, string workingDir)
		{
			var trimmedInput = currentInput.Trim();
			var fileName = trimmedInput;
			var arguments = string.Empty;
			if (trimmedInput.Contains(' '))
			{
				var positionOfBlank = trimmedInput.IndexOf(' ');
				fileName = trimmedInput.Substring(0, positionOfBlank);
				arguments = currentInput.Substring(currentInput.IndexOf(' '));
			}

			return await LaunchHelper.LaunchAppAsync(fileName, arguments, workingDir);
		}

		public async Task SetAddressBarSuggestionsAsync(AutoSuggestBox sender, IShellPage shellpage, int maxSuggestions = 7)
		{
			if (!string.IsNullOrWhiteSpace(sender.Text) && shellpage.FilesystemViewModel is not null)
			{
				if (!await SafetyExtensions.IgnoreExceptions(async () =>
				{
					IList<NavigationBarSuggestionItem>? suggestions = null;

					if (sender.Text.StartsWith(">"))
					{
						IsCommandPaletteOpen = true;
						var searchText = sender.Text.Substring(1).Trim();
						suggestions = Commands.Where(command => command.IsExecutable &&
							(command.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
							|| command.Code.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase)))
						.Select(command => new NavigationBarSuggestionItem()
						{
							Text = ">" + command.Code,
							PrimaryDisplay = command.Description,
							SupplementaryDisplay = command.HotKeyText,
							SearchText = searchText,
						}).ToList();
					}
					else
					{
						IsCommandPaletteOpen = false;
						var isFtp = FtpHelpers.IsFtpPath(sender.Text);
						var expandedPath = StorageFileExtensions.GetResolvedPath(sender.Text, isFtp);
						var folderPath = PathNormalization.GetParentDir(expandedPath) ?? expandedPath;
						StorageFolderWithPath folder = await shellpage.FilesystemViewModel.GetFolderWithPathFromPathAsync(folderPath);

						if (folder is null)
							return false;

						var currPath = await folder.GetFoldersWithPathAsync(Path.GetFileName(expandedPath), (uint)maxSuggestions);
						if (currPath.Count >= maxSuggestions)
						{
							suggestions = currPath.Select(x => new NavigationBarSuggestionItem()
							{
								Text = x.Path,
								PrimaryDisplay = x.Item.DisplayName
							}).ToList();
						}
						else if (currPath.Any())
						{
							var subPath = await currPath.First().GetFoldersWithPathAsync((uint)(maxSuggestions - currPath.Count));
							suggestions = currPath.Select(x => new NavigationBarSuggestionItem()
							{
								Text = x.Path,
								PrimaryDisplay = x.Item.DisplayName
							}).Concat(
								subPath.Select(x => new NavigationBarSuggestionItem()
								{
									Text = x.Path,
									PrimaryDisplay = PathNormalization.Combine(currPath.First().Item.DisplayName, x.Item.DisplayName)
								})).ToList();
						}
					}

					if (suggestions is null || suggestions.Count == 0)
					{
						suggestions = new List<NavigationBarSuggestionItem>() { new NavigationBarSuggestionItem() {
						Text = shellpage.FilesystemViewModel.WorkingDirectory,
						PrimaryDisplay = "NavigationToolbarVisiblePathNoResults".GetLocalizedResource() } };
					}

					// NavigationBarSuggestions becoming empty causes flickering of the suggestion box
					// Here we check whether at least an element is in common between old and new list
					if (!NavigationBarSuggestions.IntersectBy(suggestions, x => x.PrimaryDisplay).Any())
					{
						// No elements in common, update the list in-place
						for (int index = 0; index < suggestions.Count; index++)
						{
							if (index < NavigationBarSuggestions.Count)
							{
								NavigationBarSuggestions[index].Text = suggestions[index].Text;
								NavigationBarSuggestions[index].PrimaryDisplay = suggestions[index].PrimaryDisplay;
								NavigationBarSuggestions[index].SecondaryDisplay = suggestions[index].SecondaryDisplay;
								NavigationBarSuggestions[index].SupplementaryDisplay = suggestions[index].SupplementaryDisplay;
								NavigationBarSuggestions[index].SearchText = suggestions[index].SearchText;
							}
							else
							{
								NavigationBarSuggestions.Add(suggestions[index]);
							}
						}

						while (NavigationBarSuggestions.Count > suggestions.Count)
							NavigationBarSuggestions.RemoveAt(NavigationBarSuggestions.Count - 1);
					}
					else
					{
						// At least an element in common, show animation
						foreach (var s in NavigationBarSuggestions.ExceptBy(suggestions, x => x.PrimaryDisplay).ToList())
							NavigationBarSuggestions.Remove(s);

						for (int index = 0; index < suggestions.Count; index++)
						{
							if (NavigationBarSuggestions.Count > index && NavigationBarSuggestions[index].PrimaryDisplay == suggestions[index].PrimaryDisplay)
								NavigationBarSuggestions[index].SearchText = suggestions[index].SearchText;
							else
								NavigationBarSuggestions.Insert(index, suggestions[index]);
						}
					}

					return true;
				}))
				{
					NavigationBarSuggestions.Clear();
					NavigationBarSuggestions.Add(new NavigationBarSuggestionItem()
					{
						Text = shellpage.FilesystemViewModel.WorkingDirectory,
						PrimaryDisplay = "NavigationToolbarVisiblePathNoResults".GetLocalizedResource()
					});
				}
			}
		}

		private void FolderSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(FolderSettingsViewModel.GridViewSize):
				case nameof(FolderSettingsViewModel.LayoutMode):
					LayoutOpacityIcon = _InstanceViewModel.FolderSettings.LayoutMode switch
					{
						FolderLayoutModes.TilesView => Commands.LayoutTiles.OpacityStyle!,
						FolderLayoutModes.ColumnView => Commands.LayoutColumns.OpacityStyle!,
						FolderLayoutModes.GridView =>
							_InstanceViewModel.FolderSettings.GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeSmall
								? Commands.LayoutGridSmall.OpacityStyle!
								: _InstanceViewModel.FolderSettings.GridViewSize <= Constants.Browser.GridViewBrowser.GridViewSizeMedium
									? Commands.LayoutGridMedium.OpacityStyle!
									: Commands.LayoutGridLarge.OpacityStyle!,
						_ => Commands.LayoutDetails.OpacityStyle!
					};
					OnPropertyChanged(nameof(IsTilesLayout));
					OnPropertyChanged(nameof(IsColumnLayout));
					OnPropertyChanged(nameof(IsGridSmallLayout));
					OnPropertyChanged(nameof(IsGridMediumLayout));
					OnPropertyChanged(nameof(IsGridLargeLayout));
					OnPropertyChanged(nameof(IsDetailsLayout));
					break;
			}
		}

		public void Dispose()
		{
			SearchBox.Escaped -= SearchRegion_Escaped;
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;
		}
	}
}
