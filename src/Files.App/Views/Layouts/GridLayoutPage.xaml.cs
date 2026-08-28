// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.UserControls.Selection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using WinRT;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the browser page of Grid View
	/// </summary>
	[WinRT.GeneratedBindableCustomProperty(
		[
			nameof(ItemWidthGridView),
			nameof(GridViewIconSize),
			nameof(RowHeightListView),
			nameof(IconBoxSizeListView),
			nameof(CardsViewOrientation),
			nameof(CardsViewIconBoxWidth),
			nameof(CardsViewIconBoxHeight),
			nameof(CardsViewIconSize),
			nameof(CardsViewDetailsBoxWidth),
			nameof(CardsViewDetailsBoxHeight),
			nameof(CardsViewItemNameMaxLines),
			nameof(CardsViewShowContextualProperty),
			nameof(InstanceViewModel),
		],
		[])]
	public sealed partial class GridLayoutPage : BaseGroupableLayoutPage
	{
		// Fields

		/// <summary>
		/// This reference is used to prevent unnecessary icon reloading by only reloading icons when their
		/// size changes, even if the layout size changes (since some layout sizes share the same icon size).
		/// </summary>
		private uint currentIconSize;

		private volatile bool shouldSetVerticalScrollMode;

		// Properties

		public ScrollViewer? ContentScroller { get; private set; }

		protected override ListViewBase ListViewBase => FileList;
		protected override SemanticZoom RootZoom => RootGridZoom;


		// List View properties

		/// <summary>
		/// Row height in the List View layout
		/// </summary>
		public int RowHeightListView =>
			LayoutSizeKindHelper.GetListViewRowHeight(LayoutSettingsService.ListViewSize);

		/// <summary>
		/// Icon Box size in the List View layout. The value is increased by 4px to account for icon overlays.
		/// </summary>
		public int IconBoxSizeListView =>
			(int)(LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.ListView) + 4);



		// Grid View properties

		/// <summary>
		/// Item width in the Grid View layout
		/// </summary>
		public int ItemWidthGridView =>
			LayoutSizeKindHelper.GetGridViewItemWidth(LayoutSettingsService.GridViewSize);

		/// <summary>
		/// Gets the icon size for items in the Grid View layout.
		/// </summary>
		public int GridViewIconSize =>
			(int)LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.GridView);



		// Cards View properties

		/// <summary>
		/// Gets the details box width for the Cards View layout based on the card size.
		/// </summary>
		public int CardsViewDetailsBoxWidth => LayoutSettingsService.CardsViewSize switch
		{
			CardsViewSizeKind.Small => 196,
			CardsViewSizeKind.Medium => 240,
			CardsViewSizeKind.Large => 280,
			CardsViewSizeKind.ExtraLarge => 320,
			_ => 300
		};

		/// <summary>
		/// Gets the details box height for the Cards View layout based on the card size.
		/// </summary>
		public int CardsViewDetailsBoxHeight => LayoutSettingsService.CardsViewSize switch
		{
			CardsViewSizeKind.Small => 104,
			CardsViewSizeKind.Medium => 144,
			CardsViewSizeKind.Large => 144,
			CardsViewSizeKind.ExtraLarge => 128,
			_ => 128
		};

		/// <summary>
		/// Gets the icon box height for the Cards View layout based on the card size.
		/// </summary>
		public int CardsViewIconBoxHeight => LayoutSettingsService.CardsViewSize switch
		{
			CardsViewSizeKind.Small => 104,
			CardsViewSizeKind.Medium => 96,
			CardsViewSizeKind.Large => 128,
			CardsViewSizeKind.ExtraLarge => 160,
			_ => 128
		};

		/// <summary>
		/// Gets the icon box width for the Cards View layout based on the card size.
		/// </summary>
		public int CardsViewIconBoxWidth => LayoutSettingsService.CardsViewSize switch
		{
			CardsViewSizeKind.Small => 104,
			CardsViewSizeKind.Medium => 240,
			CardsViewSizeKind.Large => 280,
			CardsViewSizeKind.ExtraLarge => 320,
			_ => 128
		};

		/// <summary>
		/// Gets the orientation of cards in the Cards View layout.
		/// </summary>
		public Orientation CardsViewOrientation => UserSettingsService.LayoutSettingsService.CardsViewSize == CardsViewSizeKind.Small
			? Orientation.Horizontal
			: Orientation.Vertical;

		/// <summary>
		/// Gets the maximum lines for item names in the Cards View layout.
		/// </summary>
		public int CardsViewItemNameMaxLines =>
			LayoutSettingsService.CardsViewSize == CardsViewSizeKind.ExtraLarge ? 1 : 2;

		/// <summary>
		/// Gets the visibility for the contextual property string in the Cards View layout.
		/// </summary>
		public bool CardsViewShowContextualProperty =>
			LayoutSettingsService.CardsViewSize != CardsViewSizeKind.Small;

		/// <summary>
		/// Gets the icon size for items in the Cards View layout.
		/// </summary>
		public int CardsViewIconSize =>
			(int)LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.CardsView);



		// Constructor

		public GridLayoutPage() : base()
		{
			InitializeComponent();
			DataContext = this;

			var selectionRectangle = RectangleSelection.Create(ListViewBase, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionStarted += SelectionRectangle_SelectionStarted;
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
		}

		// Methods

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			FileList.ScrollIntoView(e);
		}

		protected override void ItemManipulationModel_ScrollToTopInvoked(object? sender, EventArgs e)
		{
			if (FolderSettings?.LayoutMode is FolderLayoutModes.ListView)
				ContentScroller?.ChangeView(0, null, null, true);
			else
				ContentScroller?.ChangeView(null, 0, null, true);
		}

		[DynamicWindowsRuntimeCast(typeof(GridViewItem))]
		protected override void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Any())
			{
				FileList.ScrollIntoView(SelectedItems.Last());
				(FileList.ContainerFromItem(SelectedItems.Last()) as GridViewItem)?.Focus(FocusState.Keyboard);
			}
		}

		protected override void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e)
		{
			if ((NextRenameIndex != 0 && TryStartRenameNextItem(e)) || (!FileList?.Items.Contains(e) ?? true))
				return;

			FileList!.SelectedItems.Add(e);
		}

		protected override void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e)
		{
			if (FileList?.Items.Contains(e) ?? false)
				FileList.SelectedItems.Remove(e);
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			if (eventArgs.Parameter is NavigationArguments navArgs)
				navArgs.FocusOnNavigation = true;

			base.OnNavigatedTo(eventArgs);

			var parentShellPage = ParentShellPageInstance
				?? throw new InvalidOperationException("The grid layout must be associated with a shell page.");
			var shellViewModel = parentShellPage.GetRequiredShellViewModel();
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The grid layout requires folder settings.");

			currentIconSize = LayoutSizeKindHelper.GetIconSize(folderSettings.LayoutMode);

			folderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			folderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
			UserSettingsService.LayoutSettingsService.PropertyChanged += LayoutSettingsService_PropertyChanged;

			// Set ItemTemplate
			SetItemTemplate();
			SetItemContainerStyle();
			FileList.ItemsSource ??= shellViewModel.FilesAndFolders;

			var parameters = (NavigationArguments)eventArgs.Parameter;
			if (parameters.IsLayoutSwitch)
				_ = ReloadItemIconsAsync();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			if (FolderSettings != null)
				FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;

			UserSettingsService.LayoutSettingsService.PropertyChanged -= LayoutSettingsService_PropertyChanged;
		}

		public override void Dispose()
		{
			Bindings.StopTracking();
			if (FolderSettings is not null)
				FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;

			UserSettingsService.LayoutSettingsService.PropertyChanged -= LayoutSettingsService_PropertyChanged;
			base.Dispose();
		}

		private void LayoutSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Get current scroll position
			var previousHorizontalOffset = ContentScroller?.HorizontalOffset;
			var previousVerticalOffset = ContentScroller?.VerticalOffset;

			if (e.PropertyName == nameof(ILayoutSettingsService.ListViewSize))
			{
				NotifyPropertyChanged(nameof(RowHeightListView));
				NotifyPropertyChanged(nameof(IconBoxSizeListView));

				// Update the container style to match the item size
				SetItemContainerStyle();
				FolderSettings_IconSizeChanged();
			}
			if (e.PropertyName == nameof(ILayoutSettingsService.CardsViewSize))
			{
				// Update the container style to match the item size
				SetItemContainerStyle();
				FolderSettings_IconSizeChanged();
			}
			if (e.PropertyName == nameof(ILayoutSettingsService.GridViewSize))
			{
				NotifyPropertyChanged(nameof(GridViewIconSize));

				// Update the container style to match the item size
				SetItemContainerStyle();
				FolderSettings_IconSizeChanged();
			}

			// Restore correct scroll position
			ContentScroller?.ChangeView(previousHorizontalOffset, previousVerticalOffset, null);
		}

		private void FolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The grid layout does not have folder settings.");

			if (folderSettings.LayoutMode == FolderLayoutModes.ListView
				|| folderSettings.LayoutMode == FolderLayoutModes.CardsView
				|| folderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				// SetItemTemplate clears FileList.ItemsSource on style swap, which drops the selection
				var preservedSelection = SelectedItems?.ToList();

				// Set ItemTemplate
				SetItemTemplate();
				SetItemContainerStyle();
				FolderSettings_IconSizeChanged();

				if (preservedSelection is { Count: > 0 })
				{
					_ = DispatcherQueue.EnqueueOrInvokeAsync(async () =>
					{
						// Wait for the new template's containers to be realized
						await Task.Delay(100);
						ItemManipulationModel.SetSelectedItems(preservedSelection);
						ItemManipulationModel.FocusSelectedItems();
					});
				}
			}
		}

		[DynamicWindowsRuntimeCast(typeof(Style))]
		private void SetItemTemplate()
		{
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The grid layout does not have folder settings.");

			var newFileListStyle = folderSettings.LayoutMode switch
			{
				FolderLayoutModes.ListView => (Style)Resources["VerticalLayoutGridView"],
				FolderLayoutModes.CardsView => (Style)Resources["HorizontalLayoutGridView"],
				_ => (Style)Resources["HorizontalLayoutGridView"]
			};

			if (FileList.Style != newFileListStyle)
			{
				var oldSource = FileList.ItemsSource;
				FileList.ItemsSource = null;
				FileList.Style = newFileListStyle;
				FileList.ItemsSource = oldSource;
			}

			shouldSetVerticalScrollMode = true;

			switch (folderSettings.LayoutMode)
			{
				case FolderLayoutModes.ListView:
					FileList.ItemTemplate = ListViewBrowserTemplate;
					break;
				case FolderLayoutModes.CardsView:
					FileList.ItemTemplate = CardsBrowserTemplate;
					break;
				default:
					FileList.ItemTemplate = GridViewBrowserTemplate;
					break;
			}
		}

		private void SetItemContainerStyle()
		{
			if (FolderSettings?.LayoutMode == FolderLayoutModes.CardsView || FolderSettings?.LayoutMode == FolderLayoutModes.GridView)
			{
				// Toggle style to force item size to update
				FileList.ItemContainerStyle = LocalListItemContainerStyle;

				// Set correct style
				FileList.ItemContainerStyle = LocalRegularItemContainerStyle;
			}
			else if (FolderSettings?.LayoutMode == FolderLayoutModes.ListView)
			{
				if (UserSettingsService.LayoutSettingsService.ListViewSize == ListViewSizeKind.Compact)
				{
					// Toggle style to force item size to update
					FileList.ItemContainerStyle = LocalRegularItemContainerStyle;

					// Set correct style
					FileList.ItemContainerStyle = LocalCompactListItemContainerStyle;
				}
				else
				{
					// Toggle style to force item size to update
					FileList.ItemContainerStyle = LocalCompactListItemContainerStyle;

					// Set correct style
					FileList.ItemContainerStyle = LocalListItemContainerStyle;
				}
			}
		}

		private void FileList_Loaded(object sender, RoutedEventArgs e)
		{
			ContentScroller = FileList.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer");
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			foreach (var item in e.AddedItems)
				SetCheckboxSelectionState(item);

			foreach (var item in e.RemovedItems)
				SetCheckboxSelectionState(item);
		}

		[DynamicWindowsRuntimeCast(typeof(GridViewItem))]
		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
		[DynamicWindowsRuntimeCast(typeof(Popup))]
		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		override public void StartRenameItem()
		{
			RenamingItem = SelectedItem;
			if (RenamingItem is null || FolderSettings is null)
				return;

			int extensionLength = RenamingItem.FileExtension?.Length ?? 0;

			if (FileList.ContainerFromItem(RenamingItem) is not GridViewItem gridViewItem)
				return;

			if (gridViewItem.FindDescendant("ItemName") is not TextBlock textBlock)
				return;

			TextBox? textBox = null;
			string editText = ShouldShowExtensionInRename(RenamingItem) ? RenamingItem.ItemNameRaw! : textBlock.Text;
			var templateRoot = gridViewItem.ContentTemplateRoot as FrameworkElement;

			// Grid View
			if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				// FindName from inside the template's namescope realizes the x:Load-deferred popup
				if (textBlock.FindName("EditPopup") is not Popup popup)
					return;

				textBox = popup.Child as TextBox;
				if (textBox is null)
					return;

				textBox.Width = templateRoot?.ActualWidth ?? gridViewItem.ActualWidth;
				textBox.Text = editText;
				textBlock.Opacity = 0;
				popup.IsOpen = true;
				OldItemName = editText;
			}
			// List View
			else if (FolderSettings.LayoutMode == FolderLayoutModes.ListView)
			{
				// FindName from inside the template's namescope realizes the x:Load-deferred text box
				textBox = textBlock.FindName("ListViewTextBoxItemName") as TextBox;
				if (textBox is null)
					return;

				textBox.Text = editText;
				OldItemName = editText;
				textBlock.Visibility = Visibility.Collapsed;
				textBox.Visibility = Visibility.Visible;

				if (textBox.FindParent<Grid>() is null)
				{
					textBlock.Visibility = Visibility.Visible;
					textBox.Visibility = Visibility.Collapsed;
					return;
				}
			}
			// Cards View
			else
			{
				textBox = gridViewItem.FindDescendant("TileViewTextBoxItemName") as TextBox;
				if (textBox is null)
					return;

				textBox.Text = editText;
				OldItemName = editText;
				textBox.Visibility = Visibility.Visible;

				if (textBox.FindParent<Grid>() is null)
				{
					textBox.Visibility = Visibility.Collapsed;
					return;
				}
			}

			var activeTextBox = textBox
				?? throw new InvalidOperationException("The rename text box is not available for the selected layout.");
			activeTextBox.Focus(FocusState.Pointer);
			activeTextBox.LostFocus += RenameTextBox_LostFocus;
			activeTextBox.KeyDown += RenameTextBox_KeyDown;

			int selectedTextLength = editText.Length;
			if (!RenamingItem.IsShortcut && (ShouldShowExtensionInRename(RenamingItem) || UserSettingsService.FoldersSettingsService.ShowFileExtensions))
				selectedTextLength -= extensionLength;

			activeTextBox.Select(0, selectedTextLength);
			IsRenamingItem = true;
		}

		private void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
		{
			if (!IsRenamingItem)
				return;

			_ = ValidateItemNameInputTextAsync(textBox, args, (showError) =>
			{
				FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
				FileNameTeachingTip.IsOpen = showError;
			});
		}

		[DynamicWindowsRuntimeCast(typeof(GridViewItem))]
		[DynamicWindowsRuntimeCast(typeof(Popup))]
		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
		protected override void EndRename(TextBox textBox)
		{
			GridViewItem? gridViewItem = FileList.ContainerFromItem(RenamingItem) as GridViewItem;

			if (textBox is null || gridViewItem is null)
			{
				// NOTE: Navigating away, do nothing
			}
			else
			{
				var layoutMode = (FolderSettings
					?? throw new InvalidOperationException("The grid layout does not have folder settings."))
					.LayoutMode;
				if (layoutMode == FolderLayoutModes.GridView)
				{
					Popup? popup = gridViewItem.FindDescendant("EditPopup") as Popup;
					TextBlock? textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;

					if (popup is not null)
						popup.IsOpen = false;

					if (textBlock is not null)
					{
						var item = textBlock.DataContext as ListedItem
							?? throw new InvalidOperationException("The renamed item is not available.");
						textBlock.Opacity = item.Opacity;
					}
				}
				else if (layoutMode is FolderLayoutModes.CardsView or FolderLayoutModes.ListView)
				{
					TextBlock? textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;

					textBox.Visibility = Visibility.Collapsed;

					if (textBlock is not null)
						textBlock.Visibility = Visibility.Visible;
				}
			}

			// Unsubscribe from events
			if (textBox is not null)
			{
				textBox.LostFocus -= RenameTextBox_LostFocus;
				textBox.KeyDown -= RenameTextBox_KeyDown;
			}

			FileNameTeachingTip.IsOpen = false;
			IsRenamingItem = false;

			// Re-focus selected list item
			gridViewItem?.Focus(FocusState.Programmatic);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(HyperlinkButton))]
		protected override async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (ParentShellPageInstance is null || IsRenamingItem)
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var focusedElement = FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot) as FrameworkElement;
			var isFooterFocused = focusedElement is HyperlinkButton;

			if (ctrlPressed && e.Key is VirtualKey.A)
			{
				e.Handled = true;

				var commands = Ioc.Default.GetRequiredService<ICommandManager>();
				var hotKey = new HotKey(Keys.A, KeyModifiers.Ctrl);

				await commands[hotKey].ExecuteAsync();
			}
			else if (e.Key == VirtualKey.Enter && !isFooterFocused && !e.KeyStatus.IsMenuKeyDown)
			{
				e.Handled = true;

				if (ctrlPressed && !shiftPressed)
				{
					var selectedItems = ParentShellPageInstance?.SlimContentPage?.SelectedItems
						?? throw new InvalidOperationException("The selected items are not available.");

					foreach (var folder in selectedItems.Where(file => file.PrimaryItemAttribute == StorageItemTypes.Folder))
					{
						await NavigationHelpers.OpenPathInNewTab(folder.ItemPath);
					}
				}
				else if (ctrlPressed && shiftPressed)
				{
					if (ParentShellPageInstance is { } parentShellPage &&
						SelectedItems.FirstOrDefault(item => item.PrimaryItemAttribute == StorageItemTypes.Folder) is { } folder)
					{
						NavigationHelpers.OpenInSecondaryPane(parentShellPage, folder);
					}
				}
			}
			else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
			{
				FilePropertiesHelpers.OpenPropertiesWindow(ParentShellPageInstance);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Space)
			{
				e.Handled = true;
			}
			else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
			{
				// Unfocus the GridView so keyboard shortcut can be handled
				Focus(FocusState.Pointer);
			}
			else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
			{
				// Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
				Focus(FocusState.Pointer);
			}
			else if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
			{
				// If list has only one item, select it on arrow down/up (#5681)
				if (IsItemSelected)
					return;

				FileList.SelectedIndex = 0;
				e.Handled = true;
			}
		}

		[DynamicWindowsRuntimeCast(typeof(GridViewItem))]
		protected override bool CanGetItemFromElement(object element)
			=> element is GridViewItem;

		private void FolderSettings_IconSizeChanged()
		{
			var folderSettings = FolderSettings
				?? throw new InvalidOperationException("The grid layout does not have folder settings.");

			// Check if icons need to be reloaded
			var newIconSize = LayoutSizeKindHelper.GetIconSize(folderSettings.LayoutMode);
			if (newIconSize != currentIconSize)
			{
				currentIconSize = newIconSize;
				_ = ReloadItemIconsAsync();
			}
		}

		private async Task ReloadItemIconsAsync()
		{
			if (ParentShellPageInstance is not { } parentShellPage)
				return;
			var shellViewModel = parentShellPage.GetRequiredShellViewModel();

			shellViewModel.CancelExtendedPropertiesLoading();
			var filesAndFolders = shellViewModel.FilesAndFolders.ToList();
			foreach (ListedItem listedItem in filesAndFolders)
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) is not null)
					await shellViewModel.LoadExtendedItemPropertiesAsync(listedItem);
			}

			if (shellViewModel.EnabledGitProperties is not GitProperties.None)
			{
				await Task.WhenAll(filesAndFolders.Select(item =>
				{
					if (item is IGitItem gitItem)
						return shellViewModel.LoadGitPropertiesAsync(gitItem);

					return Task.CompletedTask;
				}));
			}
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		[DynamicWindowsRuntimeCast(typeof(Rectangle))]
		[DynamicWindowsRuntimeCast(typeof(TextBlock))]
		[DynamicWindowsRuntimeCast(typeof(GridViewItem))]
		[DynamicWindowsRuntimeCast(typeof(Popup))]
		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private async void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
		{
			var clickedItem = e.OriginalSource as FrameworkElement;
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

			var item = (e.OriginalSource as FrameworkElement)?.DataContext as ListedItem;
			if (item is null)
			{
				// Clear selection when clicking empty area via touch
				// https://github.com/files-community/Files/issues/15051
				if (e.PointerDeviceType == PointerDeviceType.Touch)
					ItemManipulationModel.ClearSelection();

				return;
			}

			// Skip code if the control or shift key is pressed or if the user is using multiselect
			if (ctrlPressed ||
				shiftPressed ||
				clickedItem is Microsoft.UI.Xaml.Shapes.Rectangle)
			{
				e.Handled = true;
				return;
			}

			// Check if the setting to open items with a single click is turned on
			if ((item.PrimaryItemAttribute is StorageItemTypes.File && UserSettingsService.FoldersSettingsService.OpenFilesWithSingleClick.ShouldOpenWithSingleClick(e.PointerDeviceType)) ||
				(item.PrimaryItemAttribute is StorageItemTypes.Folder && UserSettingsService.FoldersSettingsService.OpenFoldersWithSingleClick.ShouldOpenWithSingleClick(e.PointerDeviceType)))
			{
				ResetRenameDoubleClick();
				await Commands.OpenItem.ExecuteAsync();
			}
			else
			{
				if (clickedItem is TextBlock textBlock && textBlock.Name == "ItemName")
				{
					CheckRenameDoubleClick(textBlock.DataContext);
				}
				else if (IsRenamingItem)
				{
					if (FileList.ContainerFromItem(RenamingItem) is GridViewItem gridViewItem)
					{
						var layoutMode = (FolderSettings
							?? throw new InvalidOperationException("The grid layout does not have folder settings."))
							.LayoutMode;
						if (layoutMode == FolderLayoutModes.GridView)
						{
							Popup? popup = gridViewItem.FindDescendant("EditPopup") as Popup;
							var textBox = popup?.Child as TextBox;

							if (textBox is not null)
								await CommitRenameAsync(textBox);
						}
						else
						{
							var textBox = gridViewItem.FindDescendant("TileViewTextBoxItemName") as TextBox;

							if (textBox is not null)
								await CommitRenameAsync(textBox);
						}
					}
				}
			}
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Skip opening selected items if the double tap doesn't capture an item
			if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem item &&
				((item.PrimaryItemAttribute == StorageItemTypes.File && !UserSettingsService.FoldersSettingsService.OpenFilesWithSingleClick.ShouldOpenWithSingleClick(e.PointerDeviceType)) ||
				 (item.PrimaryItemAttribute == StorageItemTypes.Folder && !UserSettingsService.FoldersSettingsService.OpenFoldersWithSingleClick.ShouldOpenWithSingleClick(e.PointerDeviceType))))
				await Commands.OpenItem.ExecuteAsync();
			else if ((e.OriginalSource as FrameworkElement)?.DataContext is not ListedItem && UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
				await Commands.NavigateUp.ExecuteAsync();

			ResetRenameDoubleClick();
		}

		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		private void ItemSelected_Checked(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox &&
				checkBox.DataContext is ListedItem item &&
				!FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Add(item);
		}

		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		private void ItemSelected_Unchecked(object sender, RoutedEventArgs e)
		{
			if (sender is not CheckBox checkBox)
				return;

			if (checkBox.DataContext is ListedItem item && FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Remove(item);

			// Workaround for #17298
			checkBox.IsTabStop = false;
			checkBox.IsEnabled = false;
			checkBox.IsEnabled = true;
			checkBox.IsTabStop = true;
			FileList.Focus(FocusState.Programmatic);
		}

		private readonly System.Runtime.CompilerServices.ConditionalWeakTable<SelectorItem, Tuple<object?, CheckBox>> selectionCheckboxCache = new();

		// The template-root identity check invalidates the cache when a container is re-templated
		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		private CheckBox GetSelectionCheckbox(SelectorItem container)
		{
			var root = container.ContentTemplateRoot;
			if (selectionCheckboxCache.TryGetValue(container, out var cached) && ReferenceEquals(cached.Item1, root))
				return cached.Item2;

			var checkbox = (CheckBox)container.FindDescendant("SelectionCheckbox")!;
			selectionCheckboxCache.AddOrUpdate(container, new Tuple<object?, CheckBox>(root, checkbox));
			return checkbox;
		}

		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		[DynamicWindowsRuntimeCast(typeof(GridViewItem))]
		private new void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			var selectionCheckbox = GetSelectionCheckbox(args.ItemContainer);

			selectionCheckbox.PointerEntered -= SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited -= SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled -= SelectionCheckbox_PointerCanceled;
			selectionCheckbox.Checked -= ItemSelected_Checked;
			selectionCheckbox.Unchecked -= ItemSelected_Unchecked;

			base.FileList_ContainerContentChanging(sender, args);
			if (args.InRecycleQueue)
				return;

			SetCheckboxSelectionState(args.Item, args.ItemContainer as GridViewItem);

			selectionCheckbox.PointerEntered += SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited += SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled += SelectionCheckbox_PointerCanceled;
		}

		[DynamicWindowsRuntimeCast(typeof(GridViewItem))]
		[DynamicWindowsRuntimeCast(typeof(CheckBox))]
		private void SetCheckboxSelectionState(object item, GridViewItem? lviContainer = null)
		{
			var container = lviContainer ?? FileList.ContainerFromItem(item) as GridViewItem;
			if (container is not null)
			{
				var checkbox = GetSelectionCheckbox(container);
				if (checkbox is not null)
				{
					// Temporarily disable events to avoid selecting wrong items
					checkbox.Checked -= ItemSelected_Checked;
					checkbox.Unchecked -= ItemSelected_Unchecked;

					checkbox.IsChecked = FileList.SelectedItems.Contains(item);

					checkbox.Checked += ItemSelected_Checked;
					checkbox.Unchecked += ItemSelected_Unchecked;
				}

				UpdateCheckboxVisibility(container, checkbox?.IsPointerOver ?? false);
			}
		}

		[DynamicWindowsRuntimeCast(typeof(Grid))]
		[DynamicWindowsRuntimeCast(typeof(GridViewItem))]
		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			// This is the best way I could find to set the context flyout, as doing it in the styles isn't possible
			// because you can't use bindings in the setters
			DependencyObject item = VisualTreeHelper.GetParent(sender as Grid);

			while (item is not GridViewItem)
				item = VisualTreeHelper.GetParent(item);

			if (item is GridViewItem itemContainer)
				itemContainer.ContextFlyout = ItemContextMenuFlyout;

			// Set VerticalScrollMode after an item has been loaded (#14785)
			if (shouldSetVerticalScrollMode)
			{
				shouldSetVerticalScrollMode = false;

				if (FolderSettings?.LayoutMode is FolderLayoutModes.ListView)
					ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Disabled);
				else
					ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);
			}
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void SelectionCheckbox_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<GridViewItem>()!, true);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void SelectionCheckbox_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<GridViewItem>()!, false);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void SelectionCheckbox_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<GridViewItem>()!, false);
		}

		// To avoid crashes, disable scrolling when drag-and-drop if grouped. (#14484)
		private bool ShouldDisableScrollingWhenDragAndDrop =>
			FolderSettings?.LayoutMode is FolderLayoutModes.GridView or FolderLayoutModes.CardsView &&
			(ParentShellPageInstance?.ShellViewModel?.FilesAndFolders.IsGrouped ?? false);

		protected override void FileList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Disabled);

			base.FileList_DragItemsStarting(sender, e);

			if (ShouldDisableScrollingWhenDragAndDrop && e.Cancel)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);
		}

		private void ItemsLayout_DragEnter(object sender, DragEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Disabled);
		}

		private void ItemsLayout_DragLeave(object sender, DragEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);
		}

		protected override void ItemsLayout_Drop(object sender, DragEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);

			base.ItemsLayout_Drop(sender, e);
		}

		protected override void Item_Drop(object sender, DragEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);

			base.Item_Drop(sender, e);
		}

		[DynamicWindowsRuntimeCast(typeof(GridViewItem))]
		private void UpdateCheckboxVisibility(object sender, bool isPointerOver)
		{
			if (sender is GridViewItem control && control.FindDescendant<UserControl>() is UserControl userControl)
			{
				// Handle visual states
				// Show checkboxes when items are selected (as long as the setting is enabled)
				// Show checkboxes when hovering over the checkbox area (regardless of the setting to hide them)
				if (UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems && control.IsSelected
					|| isPointerOver)
					VisualStateManager.GoToState(userControl, "ShowCheckbox", true);
				else
					VisualStateManager.GoToState(userControl, "HideCheckbox", true);
			}
		}
	}
}
