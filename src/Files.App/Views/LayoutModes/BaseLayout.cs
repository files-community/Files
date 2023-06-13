// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.Filesystem.StorageItems;
using Files.App.Helpers.ContextFlyouts;
using Files.App.ServicesImplementation;
using Files.App.Storage.FtpStorage;
using Files.App.UserControls;
using Files.App.UserControls.Menus;
using Files.App.Views;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs;
using Files.Sdk.Storage;
using Files.Sdk.Storage.LocatableStorage;
using FluentFTP;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using static Files.App.Helpers.PathNormalization;
using static Vanara.PInvoke.Shell32;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using FtpHelpers = Files.App.Helpers.FtpHelpers;
using SortDirection = Files.Shared.Enums.SortDirection;
using VanaraWindowsShell = Vanara.Windows.Shell;

namespace Files.App.Views.LayoutModes
{
	/// <summary>
	/// Represents the base class which every layout page must derive from
	/// </summary>
	public abstract class BaseLayout : Page, IBaseLayout, INotifyPropertyChanged
	{
		private readonly DispatcherQueueTimer jumpTimer;

		private IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>()!;

		protected IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>()!;

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

		public FolderSettingsViewModel? FolderSettings
			=> ParentShellPageInstance?.InstanceViewModel.FolderSettings;

		public CurrentInstanceViewModel? InstanceViewModel
			=> ParentShellPageInstance?.InstanceViewModel;

		public PreviewPaneViewModel PreviewPaneViewModel { get; private set; }
		public LayoutModeViewModel LayoutModeViewModel { get; }

		public AppModel AppModel
			=> App.AppModel;

		public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

		public CommandBarFlyout ItemContextMenuFlyout { get; set; } = new()
		{
			AlwaysExpanded = true,
			AreOpenCloseAnimationsEnabled = false,
			Placement = FlyoutPlacementMode.Right,
		};

		public CommandBarFlyout BaseContextMenuFlyout { get; set; } = new()
		{
			AlwaysExpanded = true,
			AreOpenCloseAnimationsEnabled = false,
			Placement = FlyoutPlacementMode.Right,
		};

		public BaseLayoutCommandsViewModel? CommandsViewModel { get; protected set; }

		public IShellPage? ParentShellPageInstance { get; private set; } = null;

		public bool IsRenamingItem { get; set; } = false;

		public StandardItemViewModel? RenamingItem { get; set; } = null;

		public string? OldItemName { get; set; } = null;

		private bool isMiddleClickToScrollEnabled = true;
		public bool IsMiddleClickToScrollEnabled
		{
			get => isMiddleClickToScrollEnabled;
			set
			{
				if (isMiddleClickToScrollEnabled != value)
				{
					isMiddleClickToScrollEnabled = value;

					NotifyPropertyChanged(nameof(IsMiddleClickToScrollEnabled));
				}
			}
		}

		protected AddressToolbar? NavToolbar
			=> (App.Window.Content as Frame)?.FindDescendant<AddressToolbar>();

		private CollectionViewSource collectionViewSource = new()
		{
			IsSourceGrouped = true,
		};
		public CollectionViewSource CollectionViewSource
		{
			get => collectionViewSource;
			set
			{
				if (collectionViewSource == value)
					return;

				if (collectionViewSource.View is not null)
					collectionViewSource.View.VectorChanged -= View_VectorChanged;

				collectionViewSource = value;

				NotifyPropertyChanged(nameof(CollectionViewSource));

				if (collectionViewSource.View is not null)
					collectionViewSource.View.VectorChanged += View_VectorChanged;
			}
		}

		protected NavigationArguments? navigationArguments;

		private bool isItemSelected = false;
		public bool IsItemSelected
		{
			get => isItemSelected;
			internal set
			{
				if (value != isItemSelected)
				{
					isItemSelected = value;

					NotifyPropertyChanged(nameof(IsItemSelected));
				}
			}
		}

		private string jumpString = string.Empty;
		public string JumpString
		{
			get => jumpString;
			set
			{
				// If current string is "a", and the next character typed is "a",
				// search for next file that starts with "a" (a.k.a. _jumpString = "a")
				if (jumpString.Length == 1 && value == jumpString + jumpString)
					value = jumpString;
				if (value != string.Empty)
				{
					StandardItemViewModel? jumpedToItem = null;
					StandardItemViewModel? previouslySelectedItem = IsItemSelected ? SelectedItem : null;

					// Select first matching item after currently selected item
					if (previouslySelectedItem is not null)
					{
						// Use FilesAndFolders because only displayed entries should be jumped to
						IEnumerable<StandardItemViewModel> candidateItems = LayoutModeViewModel.Items
							.SkipWhile(x => x != previouslySelectedItem)
							.Skip(value.Length == 1 ? 1 : 0) // User is trying to cycle through items starting with the same letter
							.Where(f => f.Storable.Name.Length >= value.Length && string.Equals(f.Storable.Name.Substring(0, value.Length), value, StringComparison.OrdinalIgnoreCase));
						jumpedToItem = candidateItems.FirstOrDefault();
					}

					if (jumpedToItem is null)
					{
						// Use FilesAndFolders because only displayed entries should be jumped to
						IEnumerable<StandardItemViewModel> candidateItems = LayoutModeViewModel.Items
							.Where(f => f.Storable.Name.Length >= value.Length && string.Equals(f.Storable.Name.Substring(0, value.Length), value, StringComparison.OrdinalIgnoreCase));
						jumpedToItem = candidateItems.FirstOrDefault();
					}

					if (jumpedToItem is not null)
					{
						ItemManipulationModel.SetSelectedItem(jumpedToItem);
						ItemManipulationModel.ScrollIntoView(jumpedToItem);
						ItemManipulationModel.FocusSelectedItems();
					}

					// Restart the timer
					jumpTimer.Start();
				}

				jumpString = value;
			}
		}

		private List<StandardItemViewModel>? selectedItems = new List<StandardItemViewModel>();
		public List<StandardItemViewModel>? SelectedItems
		{
			get => selectedItems;
			internal set
			{
				// Check if the new list is different then the old one
				//if (!(value?.All(x => selectedItems?.Contains(x) ?? false) ?? value == selectedItems))
				if (value != selectedItems)
				{
					if (value?.FirstOrDefault() != PreviewPaneViewModel.SelectedItem)
					{
						// Update preview pane properties
						PreviewPaneViewModel.IsItemSelected = value?.Count > 0;
						PreviewPaneViewModel.SelectedItem = value?.Count == 1 ? value.First() : null;

						// Check if the preview pane is open before updating the model
						if (PreviewPaneViewModel.IsEnabled)
						{
							var isPaneEnabled = ((App.Window.Content as Frame)?.Content as MainPage)?.ShouldPreviewPaneBeActive ?? false;
							if (isPaneEnabled)
								PreviewPaneViewModel.UpdateSelectedItemPreview();
						}
					}

					selectedItems = value;

					if (selectedItems?.Count == 0 || selectedItems?[0] is null)
					{
						IsItemSelected = false;
						SelectedItem = null;
						SelectedItemsPropertiesViewModel.IsItemSelected = false;

						ResetRenameDoubleClick();
						UpdateSelectionSize();
					}
					else if (selectedItems is not null)
					{
						IsItemSelected = true;
						SelectedItem = selectedItems.First();
						SelectedItemsPropertiesViewModel.IsItemSelected = true;

						UpdateSelectionSize();

						SelectedItemsPropertiesViewModel.SelectedItemsCount = selectedItems.Count;

						if (selectedItems.Count == 1)
						{
							SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{selectedItems.Count} {"ItemSelected/Text".GetLocalizedResource()}";
							DispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								// Tapped event must be executed first
								await Task.Delay(50);
								preRenamingItem = SelectedItem;
							});
						}
						else
						{
							SelectedItemsPropertiesViewModel.SelectedItemsCountString = $"{selectedItems!.Count} {"ItemsSelected/Text".GetLocalizedResource()}";
							ResetRenameDoubleClick();
						}
					}

					NotifyPropertyChanged(nameof(SelectedItems));
				}

				ParentShellPageInstance!.ToolbarViewModel.SelectedItems = value;
			}
		}

		public StandardItemViewModel? SelectedItem { get; private set; }

		private readonly DispatcherQueueTimer dragOverTimer, tapDebounceTimer, hoverTimer;

		protected abstract uint IconSize { get; }

		protected abstract ItemsControl ItemsControl { get; }

		public BaseLayout()
		{
			PreviewPaneViewModel = Ioc.Default.GetRequiredService<PreviewPaneViewModel>();
			LayoutModeViewModel = Ioc.Default.GetRequiredService<LayoutModeViewModel>();

			ItemManipulationModel = new ItemManipulationModel();

			HookBaseEvents();
			HookEvents();

			jumpTimer = DispatcherQueue.CreateTimer();
			jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
			jumpTimer.Tick += JumpTimer_Tick;

			SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel();
			DirectoryPropertiesViewModel = new DirectoryPropertiesViewModel();

			dragOverTimer = DispatcherQueue.CreateTimer();
			tapDebounceTimer = DispatcherQueue.CreateTimer();
			hoverTimer = DispatcherQueue.CreateTimer();
		}

		protected abstract void HookEvents();

		protected abstract void UnhookEvents();

		private void HookBaseEvents()
		{
			ItemManipulationModel.RefreshItemsOpacityInvoked += ItemManipulationModel_RefreshItemsOpacityInvoked;
		}

		private void UnhookBaseEvents()
		{
			ItemManipulationModel.RefreshItemsOpacityInvoked -= ItemManipulationModel_RefreshItemsOpacityInvoked;
		}

		public ItemManipulationModel ItemManipulationModel { get; private set; }

		private void JumpTimer_Tick(object sender, object e)
		{
			jumpString = string.Empty;
			jumpTimer.Stop();
		}

		protected abstract void InitializeCommandsViewModel();

		protected IEnumerable<StandardItemViewModel>? GetAllItems()
		{
			var items = CollectionViewSource.IsSourceGrouped
				? (CollectionViewSource.Source as BulkConcurrentObservableCollection<GroupedCollection<StandardItemViewModel>>)?.SelectMany(g => g) // add all items from each group to the new list
				: CollectionViewSource.Source as IEnumerable<StandardItemViewModel>;

			return items ?? new List<StandardItemViewModel>();
		}

		public virtual void ResetItemOpacity()
		{
			var items = GetAllItems();
			if (items is null)
				return;

			foreach (var item in items)
			{
				if (item is not null)
					item.Opacity = item.IsHiddenItem ? Constants.UI.DimItemOpacity : 1.0d;
			}
		}

		protected StandardItemViewModel? GetItemFromElement(object element)
		{
			if (element is not ContentControl item || !CanGetItemFromElement(element))
				return null;

			return (item.DataContext as StandardItemViewModel) ?? (item.Content as StandardItemViewModel) ?? (ItemsControl.ItemFromContainer(item) as StandardItemViewModel);
		}

		protected abstract bool CanGetItemFromElement(object element);

		protected virtual void BaseFolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			if (ParentShellPageInstance?.SlimContentPage is not null)
			{
				var layoutType = FolderSettings!.GetLayoutType(LayoutModeViewModel.CurrentFolder.Path);

				if (layoutType != ParentShellPageInstance.CurrentPageType)
				{
					ParentShellPageInstance.NavigateWithArguments(layoutType, new NavigationArguments()
					{
						NavPathParam = navigationArguments!.NavPathParam,
						IsSearchResultPage = navigationArguments.IsSearchResultPage,
						SearchPathParam = navigationArguments.SearchPathParam,
						SearchQuery = navigationArguments.SearchQuery,
						SearchUnindexedItems = navigationArguments.SearchUnindexedItems,
						IsLayoutSwitch = true,
						AssociatedTabInstance = ParentShellPageInstance
					});

					// Remove old layout from back stack
					ParentShellPageInstance.RemoveLastPageFromBackStack();
					ParentShellPageInstance.ResetNavigationStackLayoutMode();
				}

				LayoutModeViewModel.UpdateEmptyTextType();
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			// Add item jumping handler
			CharacterReceived -= Page_CharacterReceived;
			CharacterReceived += Page_CharacterReceived;

			navigationArguments = (NavigationArguments)eventArgs.Parameter;
			ParentShellPageInstance = navigationArguments!.AssociatedTabInstance;

			InitializeCommandsViewModel();

			IsItemSelected = false;

			FolderSettings!.LayoutModeChangeRequested -= BaseFolderSettings_LayoutModeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
			FolderSettings.GroupDirectionPreferenceUpdated -= FolderSettings_GroupDirectionPreferenceUpdated;
			FolderSettings.GroupByDateUnitPreferenceUpdated -= FolderSettings_GroupByDateUnitPreferenceUpdated;

			FolderSettings.LayoutModeChangeRequested += BaseFolderSettings_LayoutModeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
			FolderSettings.GroupDirectionPreferenceUpdated += FolderSettings_GroupDirectionPreferenceUpdated;
			FolderSettings.GroupByDateUnitPreferenceUpdated += FolderSettings_GroupByDateUnitPreferenceUpdated;

			LayoutModeViewModel.EmptyTextType = EmptyTextType.None;
			ParentShellPageInstance.ToolbarViewModel.CanRefresh = true;

			if (!navigationArguments.IsSearchResultPage)
			{
				var previousDir = LayoutModeViewModel.CurrentFolder?.Path;
				try
				{
					await LayoutModeViewModel.SetCurrentFolderFromPathAsync(navigationArguments.NavPathParam);
				}
				catch (UnauthorizedAccessException)
				{
					var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderWithPathFromPathAsync(navigationArguments.NavPathParam));
					if (res == FileSystemStatusCode.Unauthorized)
					{
						await DialogDisplayHelper.ShowDialogAsync(
							"AccessDenied".GetLocalizedResource(),
							"AccessDeniedToFolder".GetLocalizedResource());
					}
					else if (res == FileSystemStatusCode.NotFound)
					{
						await DialogDisplayHelper.ShowDialogAsync(
							"FolderNotFoundDialog/Title".GetLocalizedResource(),
							"FolderNotFoundDialog/Text".GetLocalizedResource());
					}
					else
					{
						await DialogDisplayHelper.ShowDialogAsync(
							"DriveUnpluggedDialog/Title".GetLocalizedResource(),
							res.ErrorCode.ToString());
					}

					return;
				}
				catch (FtpException)
				{
					var client = Storage.FtpStorage.FtpHelpers.GetFtpClient(navigationArguments.NavPathParam);
					if (!client.IsAuthenticated)
					{
						if (await dialogService.ShowDialogAsync<CredentialDialogViewModel>() != DialogResult.Primary)
							return;

						var credentialDialogViewModel = dialogService.GetDialog<CredentialDialogViewModel>().ViewModel;

						// Can't do more than that to mitigate immutability of strings. Perhaps convert DisposableArray to SecureString immediately?
						if (!credentialDialogViewModel.IsAnonymous)
							client.Credentials = new NetworkCredential(credentialDialogViewModel.UserName, Encoding.UTF8.GetString(credentialDialogViewModel.Password));

						OnNavigatedTo(eventArgs);
						return;
					}
				}
				var workingDir = LayoutModeViewModel.CurrentFolder!.Path;
				// pathRoot will be empty on recycle bin path
				var pathRoot = GetPathRoot(workingDir);

				var isRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = isRecycleBin;

				// Can't go up from recycle bin
				ParentShellPageInstance.ToolbarViewModel.CanNavigateToParent = !(string.IsNullOrEmpty(pathRoot) || isRecycleBin);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = false;
				ParentShellPageInstance.ToolbarViewModel.PathControlDisplayText = navigationArguments.NavPathParam;

				if (!navigationArguments.IsLayoutSwitch || previousDir != workingDir)
				{
					await LayoutModeViewModel.GetItemsAsync();
					AdaptiveLayoutHelpers.ApplyAdaptativeLayout(FolderSettings, LayoutModeViewModel.CurrentFolder.Path, LayoutModeViewModel.Items);

					if (Ioc.Default.GetRequiredService<PreviewPaneViewModel>().IsEnabled)
					{
						// Find and select README file
						foreach (var item in LayoutModeViewModel.Items)
						{
							if (item.Storable is ILocatableFile f && f.Name.Contains("readme", StringComparison.OrdinalIgnoreCase))
							{
								LayoutModeViewModel.SelectedItems.Add(item);
								break;
							}
						}
					}
				}
				else
					ParentShellPageInstance.ToolbarViewModel.CanGoForward = false;
			}
			else
			{
				await LayoutModeViewModel.SetCurrentFolderFromPathAsync(navigationArguments.SearchPathParam!);

				ParentShellPageInstance.ToolbarViewModel.CanGoForward = false;
				ParentShellPageInstance.ToolbarViewModel.CanGoBack = true;
				ParentShellPageInstance.ToolbarViewModel.CanNavigateToParent = false;

				var workingDir = LayoutModeViewModel.CurrentFolder?.Path ?? string.Empty;

				ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = true;

				if (!navigationArguments.IsLayoutSwitch)
				{
					var displayName = App.LibraryManager.TryGetLibrary(navigationArguments.SearchPathParam, out var lib) ? lib.Text : navigationArguments.SearchPathParam;
					ParentShellPageInstance.UpdatePathUIToWorkingDirectory(null, string.Format("SearchPagePathBoxOverrideText".GetLocalizedResource(), navigationArguments.SearchQuery, displayName));
					var searchInstance = new Filesystem.Search.FolderSearch
					{
						Query = navigationArguments.SearchQuery,
						Folder = navigationArguments.SearchPathParam,
						ThumbnailSize = InstanceViewModel!.FolderSettings.GetIconSize(),
						SearchUnindexedItems = navigationArguments.SearchUnindexedItems
					};

					_ = ParentShellPageInstance.FilesystemViewModel.SearchAsync(searchInstance);
				}
			}

			// Show controls that were hidden on the home page
			ParentShellPageInstance.InstanceViewModel.IsPageTypeNotHome = true;

			UpdateCollectionViewSource();
			FolderSettings.IsLayoutModeChanging = false;

			SetSelectedItemsOnNavigation();

			ItemContextMenuFlyout.Opening += ItemContextFlyout_Opening;
			BaseContextMenuFlyout.Opening += BaseContextFlyout_Opening;
		}

		public void SetSelectedItemsOnNavigation()
		{
			try
			{
				if (navigationArguments is not null &&
					navigationArguments.SelectItems is not null &&
					navigationArguments.SelectItems.Any())
				{
					List<StandardItemViewModel> liItemsToSelect = new();
					foreach (string item in navigationArguments.SelectItems)
						liItemsToSelect.Add(LayoutModeViewModel.Items.Where((li) => li.Storable.Name == item).First());

					ItemManipulationModel.SetSelectedItems(liItemsToSelect);
					ItemManipulationModel.FocusSelectedItems();
				}
				else if (navigationArguments is not null && navigationArguments.FocusOnNavigation)
				{
					// Set focus on layout specific file list control
					ItemManipulationModel.FocusFileList();
				}
			}
			catch (Exception) { }
		}

		private CancellationTokenSource? groupingCancellationToken;

		private async void FolderSettings_GroupOptionPreferenceUpdated(object? sender, GroupOption e)
		{
			await GroupPreferenceUpdated();
		}

		private async void FolderSettings_GroupDirectionPreferenceUpdated(object? sender, SortDirection e)
		{
			await GroupPreferenceUpdated();
		}

		private async void FolderSettings_GroupByDateUnitPreferenceUpdated(object? sender, GroupByDateUnit e)
		{
			await GroupPreferenceUpdated();
		}

		private async Task GroupPreferenceUpdated()
		{
			// Two or more of these running at the same time will cause a crash, so cancel the previous one before beginning
			groupingCancellationToken?.Cancel();
			groupingCancellationToken = new CancellationTokenSource();
			var token = groupingCancellationToken.Token;

			await ParentShellPageInstance!.FilesystemViewModel.GroupOptionsUpdated(token);

			UpdateCollectionViewSource();

			await ParentShellPageInstance.FilesystemViewModel.ReloadItemGroupHeaderImagesAsync();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			// Remove item jumping handler
			CharacterReceived -= Page_CharacterReceived;
			FolderSettings!.LayoutModeChangeRequested -= BaseFolderSettings_LayoutModeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
			FolderSettings.GroupDirectionPreferenceUpdated -= FolderSettings_GroupDirectionPreferenceUpdated;
			FolderSettings.GroupByDateUnitPreferenceUpdated -= FolderSettings_GroupByDateUnitPreferenceUpdated;
			ItemContextMenuFlyout.Opening -= ItemContextFlyout_Opening;
			BaseContextMenuFlyout.Opening -= BaseContextFlyout_Opening;

			var parameter = e.Parameter as NavigationArguments;
			if (parameter is not null && !parameter.IsLayoutSwitch)
				ParentShellPageInstance!.FilesystemViewModel.CancelLoadAndClearFiles();
		}

		public async void ItemContextFlyout_Opening(object? sender, object e)
		{
			App.LastOpenedFlyout = sender as CommandBarFlyout;

			try
			{
				// Workaround for item sometimes not getting selected
				if (!IsItemSelected && (sender as CommandBarFlyout)?.Target is ListViewItem { Content: StandardItemViewModel li })
					ItemManipulationModel.SetSelectedItem(li);

				if (IsItemSelected)
					await LoadMenuItemsAsync();
			}
			catch (Exception error)
			{
				Debug.WriteLine(error);
			}
		}

		private CancellationTokenSource? shellContextMenuItemCancellationToken;

		public async void BaseContextFlyout_Opening(object? sender, object e)
		{
			App.LastOpenedFlyout = sender as CommandBarFlyout;

			try
			{
				ItemManipulationModel.ClearSelection();

				// Reset menu max height
				if (BaseContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
					itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;

				shellContextMenuItemCancellationToken?.Cancel();
				shellContextMenuItemCancellationToken = new CancellationTokenSource();

				var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
				var items = ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: InstanceViewModel!, selectedItems: new List<StandardItemViewModel> { ParentShellPageInstance!.FilesystemViewModel.CurrentFolder }, commandsViewModel: CommandsViewModel!, shiftPressed: shiftPressed, itemViewModel: ParentShellPageInstance!.FilesystemViewModel, selectedItemsPropertiesViewModel: null);

				BaseContextMenuFlyout.PrimaryCommands.Clear();
				BaseContextMenuFlyout.SecondaryCommands.Clear();

				var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);

				AddCloseHandler(BaseContextMenuFlyout, primaryElements, secondaryElements);

				primaryElements.ForEach(i => BaseContextMenuFlyout.PrimaryCommands.Add(i));

				// Set menu min width
				secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);
				secondaryElements.ForEach(i => BaseContextMenuFlyout.SecondaryCommands.Add(i));

				if (!InstanceViewModel!.IsPageTypeSearchResults && !InstanceViewModel.IsPageTypeZipFolder)
				{
					var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(workingDir: ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, selectedItems: new List<StandardItemViewModel>(), shiftPressed: shiftPressed, showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
					if (shellMenuItems.Any())
						await AddShellMenuItemsAsync(shellMenuItems, BaseContextMenuFlyout, shiftPressed);
					else
						RemoveOverflow(BaseContextMenuFlyout);
				}
			}
			catch (Exception error)
			{
				Debug.WriteLine(error);
			}
		}

		public void UpdateSelectionSize()
		{
			var items = (selectedItems?.Any() ?? false) ? selectedItems : GetAllItems();
			if (items is null)
				return;

			var isSizeKnown = !items.Any(item => string.IsNullOrEmpty(item.FileSize));
			if (isSizeKnown)
			{
				long size = items.Sum(item => item.FileSizeBytes);
				SelectedItemsPropertiesViewModel.ItemSizeBytes = size;
				SelectedItemsPropertiesViewModel.ItemSize = size.ToSizeString();
			}
			else
			{
				SelectedItemsPropertiesViewModel.ItemSizeBytes = 0;
				SelectedItemsPropertiesViewModel.ItemSize = string.Empty;
			}

			SelectedItemsPropertiesViewModel.ItemSizeVisibility = isSizeKnown;
		}

		private async Task LoadMenuItemsAsync()
		{
			// Reset menu max height
			if (ItemContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
				itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;

			shellContextMenuItemCancellationToken?.Cancel();
			shellContextMenuItemCancellationToken = new CancellationTokenSource();
			SelectedItemsPropertiesViewModel.CheckAllFileExtensions(SelectedItems!.Select(selectedItem => selectedItem?.FileExtension).ToList()!);

			var shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
			var items = ContextFlyoutItemHelper.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: InstanceViewModel!, selectedItems: SelectedItems!, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: CommandsViewModel!, shiftPressed: shiftPressed, itemViewModel: null);

			ItemContextMenuFlyout.PrimaryCommands.Clear();
			ItemContextMenuFlyout.SecondaryCommands.Clear();

			var (primaryElements, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(items);
			AddCloseHandler(ItemContextMenuFlyout, primaryElements, secondaryElements);
			primaryElements.ForEach(ItemContextMenuFlyout.PrimaryCommands.Add);
			secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
			secondaryElements.ForEach(ItemContextMenuFlyout.SecondaryCommands.Add);

			if (InstanceViewModel!.CanTagFilesInPage)
				AddNewFileTagsToMenu(ItemContextMenuFlyout);

			if (!InstanceViewModel.IsPageTypeZipFolder)
			{
				var shellMenuItems = await ContextFlyoutItemHelper.GetItemContextShellCommandsAsync(workingDir: ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, selectedItems: SelectedItems!, shiftPressed: shiftPressed, showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
				if (shellMenuItems.Any())
					await AddShellMenuItemsAsync(shellMenuItems, ItemContextMenuFlyout, shiftPressed);
				else
					RemoveOverflow(ItemContextMenuFlyout);
			}
		}

		private void AddCloseHandler(CommandBarFlyout flyout, IList<ICommandBarElement> primaryElements, IList<ICommandBarElement> secondaryElements)
		{
			// Workaround for WinUI (#5508)
			var closeHandler = new RoutedEventHandler((s, e) => flyout.Hide());

			primaryElements
				.OfType<AppBarButton>()
				.ForEach(button => button.Click += closeHandler);

			var menuFlyoutItems = secondaryElements
				.OfType<AppBarButton>()
				.Select(item => item.Flyout)
				.OfType<MenuFlyout>()
				.SelectMany(menu => menu.Items);

			addCloseHandler(menuFlyoutItems);

			void addCloseHandler(IEnumerable<MenuFlyoutItemBase> menuFlyoutItems)
			{
				menuFlyoutItems.OfType<MenuFlyoutItem>()
					.ForEach(button => button.Click += closeHandler);
				menuFlyoutItems.OfType<MenuFlyoutSubItem>()
					.ForEach(menu => addCloseHandler(menu.Items));
			}
		}

		private void AddNewFileTagsToMenu(CommandBarFlyout contextMenu)
		{
			var fileTagsContextMenu = new FileTagsContextMenu(SelectedItems!);
			var overflowSeparator = contextMenu.SecondaryCommands.FirstOrDefault(x => x is FrameworkElement fe && fe.Tag as string == "OverflowSeparator") as AppBarSeparator;
			var index = contextMenu.SecondaryCommands.IndexOf(overflowSeparator);
			index = index >= 0 ? index : contextMenu.SecondaryCommands.Count;

			// Only show the edit tags flyout if settings is enabled
			if (!UserSettingsService.GeneralSettingsService.ShowEditTagsMenu)
				return;

			contextMenu.SecondaryCommands.Insert(index, new AppBarSeparator());
			contextMenu.SecondaryCommands.Insert(index + 1, new AppBarButton()
			{
				Label = "SettingsEditFileTagsExpander/Title".GetLocalizedResource(),
				Content = new OpacityIcon()
				{
					Style = (Style)Application.Current.Resources["ColorIconTag"],
				},
				Flyout = fileTagsContextMenu
			});
		}

		private async Task AddShellMenuItemsAsync(List<ContextMenuFlyoutItemViewModel> shellMenuItems, CommandBarFlyout contextMenuFlyout, bool shiftPressed)
		{
			var openWithMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });
			var sendToMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });
			var shellMenuItemsFiltered = shellMenuItems.Where(x => x != openWithMenuItem && x != sendToMenuItem).ToList();
			var mainShellMenuItems = shellMenuItemsFiltered.RemoveFrom(!UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 0);
			var overflowShellMenuItemsUnfiltered = shellMenuItemsFiltered.Except(mainShellMenuItems).ToList();
			var overflowShellMenuItems = overflowShellMenuItemsUnfiltered.Where(
				(x, i) => (x.ItemType == ContextMenuFlyoutItemType.Separator &&
				overflowShellMenuItemsUnfiltered[i + 1 < overflowShellMenuItemsUnfiltered.Count ? i + 1 : i].ItemType != ContextMenuFlyoutItemType.Separator)
				|| x.ItemType != ContextMenuFlyoutItemType.Separator).ToList();

			var overflowItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(overflowShellMenuItems);
			var mainItems = ItemModelListToContextFlyoutHelper.GetAppBarButtonsFromModelIgnorePrimary(mainShellMenuItems);

			var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(App.Window);
			var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");

			var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
			if (itemsControl is not null && secondaryMenu is not null)
			{
				contextMenuFlyout.SetValue(ContextMenuExtensions.ItemsControlProperty, itemsControl);

				var ttv = secondaryMenu.TransformToVisual(App.Window.Content);
				var cMenuPos = ttv.TransformPoint(new Point(0, 0));

				var requiredHeight = contextMenuFlyout.SecondaryCommands.Concat(mainItems).Where(x => x is not AppBarSeparator).Count() * Constants.UI.ContextMenuSecondaryItemsHeight;
				var availableHeight = App.Window.Bounds.Height - cMenuPos.Y - Constants.UI.ContextMenuPrimaryItemsHeight;

				// Set menu max height to current height (Avoid menu repositioning)
				if (requiredHeight > availableHeight)
					itemsControl.MaxHeight = Math.Min(Constants.UI.ContextMenuMaxHeight, Math.Max(itemsControl.ActualHeight, Math.Min(availableHeight, requiredHeight)));

				// Set items max width to current menu width (#5555)
				mainItems.OfType<FrameworkElement>().ForEach(x => x.MaxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin);
			}

			var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
			if (overflowItem is not null)
			{
				var overflowItemFlyout = overflowItem.Flyout as MenuFlyout;
				if (overflowItemFlyout is not null)
				{
					if (overflowItemFlyout.Items.Count > 0)
						overflowItemFlyout.Items.Insert(0, new MenuFlyoutSeparator());

					var index = contextMenuFlyout.SecondaryCommands.Count - 2;
					foreach (var i in mainItems)
					{
						index++;
						contextMenuFlyout.SecondaryCommands.Insert(index, i);
					}

					index = 0;
					foreach (var i in overflowItems)
					{
						overflowItemFlyout.Items.Insert(index, i);
						index++;
					}

					if (overflowItemFlyout.Items.Count > 0 && UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
					{
						overflowItem.Label = "ShowMoreOptions".GetLocalizedResource();
						overflowItem.IsEnabled = true;
					}
					else if (!UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
						overflowItem.Visibility = Visibility.Collapsed;
				}
			}
			else
			{
				mainItems.ForEach(x => contextMenuFlyout.SecondaryCommands.Add(x));
			}

			// Add items to openwith dropdown
			var openWithOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWithOverflow") as AppBarButton;

			var openWith = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWith") as AppBarButton;
			if (openWithMenuItem?.LoadSubMenuAction is not null && openWithOverflow is not null && openWith is not null)
			{
				await openWithMenuItem.LoadSubMenuAction();
				var openWithSubItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(ShellContextmenuHelper.GetOpenWithItems(shellMenuItems));

				if (openWithSubItems is not null)
				{
					var flyout = (MenuFlyout)openWithOverflow.Flyout;

					flyout.Items.Clear();

					foreach (var item in openWithSubItems)
						flyout.Items.Add(item);

					openWithOverflow.Flyout = flyout;
					openWith.Visibility = Visibility.Collapsed;
					openWithOverflow.Visibility = Visibility.Visible;
				}
			}

			// Add items to sendto dropdown
			var sendToOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "SendToOverflow") as AppBarButton;

			var sendTo = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "SendTo") as AppBarButton;
			if (sendToMenuItem?.LoadSubMenuAction is not null && sendToOverflow is not null && sendTo is not null)
			{
				await sendToMenuItem.LoadSubMenuAction();
				var sendToSubItems = ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(ShellContextmenuHelper.GetSendToItems(shellMenuItems));

				if (sendToSubItems is not null)
				{
					var flyout = (MenuFlyout)sendToOverflow.Flyout;

					flyout.Items.Clear();

					foreach (var item in sendToSubItems)
						flyout.Items.Add(item);

					sendToOverflow.Flyout = flyout;
					sendTo.Visibility = Visibility.Collapsed;
					sendToOverflow.Visibility = Visibility.Visible;
				}
			}

			// Add items to main shell submenu
			mainShellMenuItems.Where(x => x.LoadSubMenuAction is not null).ForEach(async x => {
				await x.LoadSubMenuAction();

				ShellContextmenuHelper.AddItemsToMainMenu(mainItems, x);
			});

			// Add items to overflow shell submenu
			overflowShellMenuItems.Where(x => x.LoadSubMenuAction is not null).ForEach(async x => {
				await x.LoadSubMenuAction();

				ShellContextmenuHelper.AddItemsToOverflowMenu(overflowItem, x);
			});

			itemsControl?.Items.OfType<FrameworkElement>().ForEach(item =>
			{
				// Enable CharacterEllipsis text trimming for menu items
				if (item.FindDescendant("OverflowTextLabel") is TextBlock label)
					label.TextTrimming = TextTrimming.CharacterEllipsis;

				// Close main menu when clicking on subitems (#5508)
				if ((item as AppBarButton)?.Flyout as MenuFlyout is MenuFlyout flyout)
				{
					Action<IList<MenuFlyoutItemBase>> clickAction = null!;
					clickAction = (items) =>
					{
						items.OfType<MenuFlyoutItem>().ForEach(i =>
						{
							i.Click += new RoutedEventHandler((s, e) => contextMenuFlyout.Hide());
						});
						items.OfType<MenuFlyoutSubItem>().ForEach(i =>
						{
							clickAction(i.Items);
						});
					};

					clickAction(flyout.Items);
				}
			});
		}

		private void RemoveOverflow(CommandBarFlyout contextMenuFlyout)
		{
			var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
			var overflowSeparator = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarSeparator appBarSeparator && (appBarSeparator.Tag as string) == "OverflowSeparator") as AppBarSeparator;

			if (overflowItem is not null)
				overflowItem.Visibility = Visibility.Collapsed;
			if (overflowSeparator is not null)
				overflowSeparator.Visibility = Visibility.Collapsed;
		}

		protected virtual void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			if (ParentShellPageInstance!.IsCurrentInstance)
			{
				char letter = args.Character;
				JumpString += letter.ToString().ToLowerInvariant();
			}
		}

		protected void FileList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			try
			{
				var shellItemList = e.Items.OfType<StandardItemViewModel>().Select(x => new VanaraWindowsShell.ShellItem(x.ItemPath)).ToArray();
				if (shellItemList[0].FileSystemPath is not null && !InstanceViewModel.IsPageTypeSearchResults)
				{
					var iddo = shellItemList[0].Parent.GetChildrenUIObjects<IDataObject>(HWND.NULL, shellItemList);
					shellItemList.ForEach(x => x.Dispose());

					var format = System.Windows.Forms.DataFormats.GetFormat("Shell IDList Array");
					if (iddo.TryGetData<byte[]>((uint)format.Id, out var data))
					{
						var mem = new MemoryStream(data).AsRandomAccessStream();
						e.Data.SetData(format.Name, mem);
					}
				}
				else
				{
					// Only support IStorageItem capable paths
					var storageItemList = e.Items.OfType<StandardItemViewModel>().Where(x => !(x.IsHiddenItem && x.IsLinkItem && x.IsRecycleBinItem && x.IsShortcut)).Select(x => VirtualStorageItem.FromListedItem(x));
					e.Data.SetStorageItems(storageItemList, false);
				}
			}
			catch (Exception)
			{
				e.Cancel = true;
			}
		}

		private StandardItemViewModel? dragOverItem = null;

		private void Item_DragLeave(object sender, DragEventArgs e)
		{
			var item = GetItemFromElement(sender);

			// Reset dragged over item
			if (item == dragOverItem)
				dragOverItem = null;
		}

		protected async void Item_DragOver(object sender, DragEventArgs e)
		{
			var item = GetItemFromElement(sender);
			if (item is null)
				return;

			DragOperationDeferral? deferral = null;

			try
			{
				deferral = e.GetDeferral();

				if (dragOverItem != item)
				{
					dragOverItem = item;
					dragOverTimer.Stop();
					dragOverTimer.Debounce(() =>
					{
						if (dragOverItem is not null && !dragOverItem.IsExecutable)
						{
							dragOverTimer.Stop();
							ItemManipulationModel.SetSelectedItem(dragOverItem);
							dragOverItem = null;
							_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance!, false);
						}
					},
					TimeSpan.FromMilliseconds(1000), false);
				}

				if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				{
					e.Handled = true;

					var draggedItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);

					if (draggedItems.Any(draggedItem => draggedItem.Path == item.ItemPath))
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else if (!draggedItems.Any())
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						e.DragUIOverride.IsCaptionVisible = true;

						if (item.IsExecutable)
						{
							e.DragUIOverride.Caption = $"{"OpenItemsWithCaptionText".GetLocalizedResource()} {item.Name}";
							e.AcceptedOperation = DataPackageOperation.Link;
						}
						// Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
						else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Link;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Move;
						}
						else if (draggedItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
							|| ZipStorageFolder.IsZipPath(item.ItemPath))
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (draggedItems.AreItemsInSameDrive(item.ItemPath))
						{
							e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Move;
						}
						else
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
					}
				}
			}
			finally
			{
				deferral?.Complete();
			}
		}

		protected async void Item_Drop(object sender, DragEventArgs e)
		{
			var deferral = e.GetDeferral();

			e.Handled = true;

			// Reset dragged over item
			dragOverItem = null;

			var item = GetItemFromElement(sender);
			if (item is not null)
				await ParentShellPageInstance!.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, (item as ShortcutItem)?.TargetPath ?? item.ItemPath, false, true, item.IsExecutable);

			deferral.Complete();
		}

		protected void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			RefreshContainer(args.ItemContainer, args.InRecycleQueue);
			RefreshItem(args.ItemContainer, args.Item, args.InRecycleQueue, args);
		}

		private void RefreshContainer(SelectorItem container, bool inRecycleQueue)
		{
			container.PointerPressed -= FileListItem_PointerPressed;
			container.PointerEntered -= FileListItem_PointerEntered;
			container.PointerExited -= FileListItem_PointerExited;
			container.RightTapped -= FileListItem_RightTapped;

			if (inRecycleQueue)
			{
				UninitializeDrag(container);
			}
			else
			{
				container.PointerPressed += FileListItem_PointerPressed;
				container.RightTapped += FileListItem_RightTapped;
				if (UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				{
					container.PointerEntered += FileListItem_PointerEntered;
					container.PointerExited += FileListItem_PointerExited;
				}
			}
		}

		private void RefreshItem(SelectorItem container, object item, bool inRecycleQueue, ContainerContentChangingEventArgs args)
		{
			if (item is not StandardItemViewModel listedItem)
				return;

			if (inRecycleQueue)
			{
				ParentShellPageInstance!.FilesystemViewModel.CancelExtendedPropertiesLoadingForItem(listedItem);
			}
			else
			{
				InitializeDrag(container, listedItem);

				if (!listedItem.ItemPropertiesInitialized)
				{
					uint callbackPhase = 3;
					args.RegisterUpdateCallback(callbackPhase, async (s, c) =>
					{
						await ParentShellPageInstance!.FilesystemViewModel.LoadExtendedItemProperties(listedItem, IconSize);
					});
				}
			}
		}

		protected static void FileListItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender is not SelectorItem selectorItem)
				return;

			if (selectorItem.IsSelected && e.KeyModifiers == VirtualKeyModifiers.Control)
			{
				selectorItem.IsSelected = false;

				// Prevent issues arising caused by the default handlers attempting to select the item that has just been deselected by ctrl + click
				e.Handled = true;
			}
			else if (!selectorItem.IsSelected && e.GetCurrentPoint(selectorItem).Properties.IsLeftButtonPressed)
			{
				selectorItem.IsSelected = true;
			}
		}

		private StandardItemViewModel? hoveredItem = null;

		protected internal void FileListItem_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (!UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				return;

			hoveredItem = GetItemFromElement(sender);

			hoverTimer.Stop();
			hoverTimer.Debounce(() =>
			{
				if (hoveredItem is null)
					return;

				hoverTimer.Stop();

				// Selection of multiple individual items with control
				if (e.KeyModifiers == VirtualKeyModifiers.Control &&
					selectedItems is not null)
				{
					ItemManipulationModel.AddSelectedItem(hoveredItem);
				}
				// Selection of a range of items with shift
				else if (e.KeyModifiers == VirtualKeyModifiers.Shift &&
					selectedItems is not null &&
					selectedItems.Any())
				{
					var last = selectedItems.Last();
					byte found = 0;
					for (int i = 0; i < ItemsControl.Items.Count && found != 2; i++)
					{
						if (ItemsControl.Items[i] == last || ItemsControl.Items[i] == hoveredItem)
							found++;

						if (found != 0 && !selectedItems.Contains(ItemsControl.Items[i]))
							ItemManipulationModel.AddSelectedItem((StandardItemViewModel)ItemsControl.Items[i]);
					}
				}
				// Avoid resetting the selection if multiple items are selected
				else if (SelectedItems is null || SelectedItems.Count <= 1)
				{
					ItemManipulationModel.SetSelectedItem(hoveredItem);
				}
			},
			TimeSpan.FromMilliseconds(600), false);
		}

		protected internal void FileListItem_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (!UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				return;

			hoverTimer.Stop();
			hoveredItem = null;
		}

		protected void FileListItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var rightClickedItem = GetItemFromElement(sender);

			if (rightClickedItem is not null && !((SelectorItem)sender).IsSelected)
				ItemManipulationModel.SetSelectedItem(rightClickedItem);
		}

		protected void InitializeDrag(UIElement containter, StandardItemViewModel item)
		{
			if (item is null)
				return;

			UninitializeDrag(containter);
			if ((item.PrimaryItemAttribute == StorageItemTypes.Folder && !RecycleBinHelpers.IsPathUnderRecycleBin(item.ItemPath)) || item.IsExecutable)
			{
				containter.AllowDrop = true;
				containter.DragOver += Item_DragOver;
				containter.DragLeave += Item_DragLeave;
				containter.Drop += Item_Drop;
			}
		}

		protected void UninitializeDrag(UIElement element)
		{
			element.AllowDrop = false;
			element.DragOver -= Item_DragOver;
			element.DragLeave -= Item_DragLeave;
			element.Drop -= Item_Drop;
		}

		// VirtualKey doesn't support or accept plus and minus by default.
		public readonly VirtualKey PlusKey = (VirtualKey)187;

		public readonly VirtualKey MinusKey = (VirtualKey)189;

		public virtual void Dispose()
		{
			PreviewPaneViewModel?.Dispose();
			UnhookBaseEvents();
		}

		protected void ItemsLayout_DragOver(object sender, DragEventArgs e)
		{
			CommandsViewModel?.DragOverCommand?.Execute(e);
		}

		protected void ItemsLayout_Drop(object sender, DragEventArgs e)
		{
			CommandsViewModel?.DropCommand?.Execute(e);
		}

		private void UpdateCollectionViewSource()
		{
			if (ParentShellPageInstance is null)
				return;

			if (ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.IsGrouped)
			{
				CollectionViewSource = new()
				{
					IsSourceGrouped = true,
					Source = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.GroupedCollection
				};
			}
			else
			{
				CollectionViewSource = new()
				{
					IsSourceGrouped = false,
					Source = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders
				};
			}
		}

		protected void SemanticZoom_ViewChangeStarted(object sender, SemanticZoomViewChangedEventArgs e)
		{
			if (e.IsSourceZoomedInView)
				return;

			// According to the docs this isn't necessary, but it would crash otherwise
			var destination = e.DestinationItem.Item as GroupedCollection<StandardItemViewModel>;

			e.DestinationItem.Item = destination?.FirstOrDefault();
		}

		protected void StackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "PointerOver", true);
		}

		protected void StackPanel_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "Normal", true);
		}

		protected void RootPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "Pressed", true);
		}

		private void ItemManipulationModel_RefreshItemsOpacityInvoked(object? sender, EventArgs e)
		{
			var items = GetAllItems();
			if (items is null)
				return;

			foreach (StandardItemViewModel listedItem in items)
			{
				if (listedItem.IsHiddenItem)
					listedItem.Opacity = Constants.UI.DimItemOpacity;
				else
					listedItem.Opacity = 1;
			}
		}

		private void View_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			if (ParentShellPageInstance is not null)
				ParentShellPageInstance.ToolbarViewModel.HasItem = CollectionViewSource.View.Any();
		}

		virtual public void StartRenameItem()
		{
		}

		private StandardItemViewModel? preRenamingItem = null;

		public void CheckRenameDoubleClick(object clickedItem)
		{
			if (clickedItem is StandardItemViewModel item)
			{
				if (item == preRenamingItem)
				{
					tapDebounceTimer.Debounce(() =>
					{
						if (item == preRenamingItem)
						{
							StartRenameItem();
							tapDebounceTimer.Stop();
						}
					},
					TimeSpan.FromMilliseconds(500));
				}
				else
				{
					tapDebounceTimer.Stop();
					preRenamingItem = item;
				}
			}
			else
			{
				ResetRenameDoubleClick();
			}
		}

		public void ResetRenameDoubleClick()
		{
			preRenamingItem = null;
			tapDebounceTimer.Stop();
		}

		protected async Task ValidateItemNameInputText(TextBox textBox, TextBoxBeforeTextChangingEventArgs args, Action<bool> showError)
		{
			if (FilesystemHelpers.ContainsRestrictedCharacters(args.NewText))
			{
				args.Cancel = true;

				await DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					var oldSelection = textBox.SelectionStart + textBox.SelectionLength;
					var oldText = textBox.Text;
					textBox.Text = FilesystemHelpers.FilterRestrictedCharacters(args.NewText);
					textBox.SelectionStart = oldSelection + textBox.Text.Length - oldText.Length;
					showError?.Invoke(true);
				});
			}
			else
			{
				showError?.Invoke(false);
			}
		}

		public class ContextMenuExtensions : DependencyObject
		{
			public static ItemsControl GetItemsControl(DependencyObject obj)
			{
				return (ItemsControl)obj.GetValue(ItemsControlProperty);
			}

			public static void SetItemsControl(DependencyObject obj, ItemsControl value)
			{
				obj.SetValue(ItemsControlProperty, value);
			}

			public static readonly DependencyProperty ItemsControlProperty =
				DependencyProperty.RegisterAttached("ItemsControl", typeof(ItemsControl), typeof(ContextMenuExtensions), new PropertyMetadata(null));
		}
	}
}
