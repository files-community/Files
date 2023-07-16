// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using Windows.Foundation;
using SortDirection = Files.Core.Data.Enums.SortDirection;

namespace Files.App.ViewModels.LayoutModes
{
	public class DetailsLayoutBrowserViewModel : ObservableObject
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		private FolderSettingsViewModel? FolderSettings
			=> context.ShellPage?.InstanceViewModel.FolderSettings;

		private CurrentInstanceViewModel? InstanceViewModel
			=> context.ShellPage?.InstanceViewModel;

		public ListViewBase ListViewBase { get; set; } = null!;

		private IList<ListedItem> ListedItems
			=> ListViewBase.Items.Cast<ListedItem>().ToList();

		public DetailsLayoutColumnItemCollection ColumnsViewModel { get; }

		private MenuFlyout _ColumnHeadersMenuFlyout;
		public MenuFlyout ColumnHeadersMenuFlyout
		{
			get => _ColumnHeadersMenuFlyout;
			private set => SetProperty(ref _ColumnHeadersMenuFlyout, value);
		}

		public IList<ColumnDefinition> ColumnHeaderDefinitionItems { get; private set; }

		public IList<FrameworkElement> ColumnHeaderItems { get; private set; }

		public ICommand ToggleColumnCommand { get; set; }

		public ICommand SetColumnsAsDefaultCommand { get; set; }

		public ICommand ResizeAllColumnsToFitCommand { get; set; }

		public ICommand UpdateSortOptionsCommand { get; }

		public DetailsLayoutBrowserViewModel()
		{
			ColumnsViewModel = new();
			InitializeColumns();

			SetColumnsVisibility(new PageTypeUpdatedEventArgs()
			{
				IsTypeCloudDrive = InstanceViewModel.IsPageTypeCloudDrive,
				IsTypeRecycleBin = InstanceViewModel.IsPageTypeRecycleBin,
				IsTypeGitRepository = InstanceViewModel.IsGitRepository,
				IsTypeSearchResults = InstanceViewModel.IsPageTypeSearchResults
			});

			ToggleColumnCommand = new RelayCommand<DetailsLayoutColumnItem>(ToggleColumn);
			SetColumnsAsDefaultCommand = new RelayCommand(SetColumnsAsDefault);
			ResizeAllColumnsToFitCommand = new RelayCommand(ResizeAllColumnsToFit);
			UpdateSortOptionsCommand = new RelayCommand<string>(UpdateSortOptions);

			ColumnHeadersMenuFlyout = new();
			var columnHeadersMenuFlyoutItems = GetColumnsHeaderContextMenuFlyout();
			columnHeadersMenuFlyoutItems.ForEach(ColumnHeadersMenuFlyout.Items.Add);

			ColumnHeaderDefinitionItems = GetColumnHeaderDefinitions();

			ColumnHeaderItems = GetColumnHeaderItems();

			SetGridColumnForColumnHeaders();
		}

		private void InitializeColumns()
		{
			if (FolderSettings.ColumnsViewModel is not null)
			{
				ColumnsViewModel.IconColumn = FolderSettings.ColumnsViewModel.IconColumn;
				ColumnsViewModel.NameColumn = FolderSettings.ColumnsViewModel.NameColumn;
				ColumnsViewModel.GitStatusColumn = FolderSettings.ColumnsViewModel.GitStatusColumn;
				ColumnsViewModel.GitLastCommitDateColumn = FolderSettings.ColumnsViewModel.GitLastCommitDateColumn;
				ColumnsViewModel.GitLastCommitMessageColumn = FolderSettings.ColumnsViewModel.GitLastCommitMessageColumn;
				ColumnsViewModel.GitCommitAuthorColumn = FolderSettings.ColumnsViewModel.GitCommitAuthorColumn;
				ColumnsViewModel.GitLastCommitShaColumn = FolderSettings.ColumnsViewModel.GitLastCommitShaColumn;
				ColumnsViewModel.TagColumn = FolderSettings.ColumnsViewModel.TagColumn;
				ColumnsViewModel.DateCreatedColumn = FolderSettings.ColumnsViewModel.DateCreatedColumn;
				ColumnsViewModel.DateDeletedColumn = FolderSettings.ColumnsViewModel.DateDeletedColumn;
				ColumnsViewModel.DateModifiedColumn = FolderSettings.ColumnsViewModel.DateModifiedColumn;
				ColumnsViewModel.ItemTypeColumn = FolderSettings.ColumnsViewModel.ItemTypeColumn;
				ColumnsViewModel.PathColumn = FolderSettings.ColumnsViewModel.PathColumn;
				ColumnsViewModel.OriginalPathColumn = FolderSettings.ColumnsViewModel.OriginalPathColumn;
				ColumnsViewModel.SizeColumn = FolderSettings.ColumnsViewModel.SizeColumn;
				ColumnsViewModel.StatusColumn = FolderSettings.ColumnsViewModel.StatusColumn;
			}
		}
		
		public void SetColumnsVisibility(PageTypeUpdatedEventArgs e)
		{
			// Show original path and date deleted columns in Recycle Bin
			if (e.IsTypeRecycleBin)
			{
				ColumnsViewModel.OriginalPathColumn.Show();
				ColumnsViewModel.DateDeletedColumn.Show();
			}
			else
			{
				ColumnsViewModel.OriginalPathColumn.Hide();
				ColumnsViewModel.DateDeletedColumn.Hide();
			}

			// Show cloud drive item status column
			if (e.IsTypeCloudDrive)
				ColumnsViewModel.StatusColumn.Show();
			else
				ColumnsViewModel.StatusColumn.Hide();

			// Show git columns in git repository
			if (e.IsTypeGitRepository)
			{
				ColumnsViewModel.GitCommitAuthorColumn.Show();
				ColumnsViewModel.GitLastCommitDateColumn.Show();
				ColumnsViewModel.GitLastCommitMessageColumn.Show();
				ColumnsViewModel.GitLastCommitShaColumn.Show();
				ColumnsViewModel.GitStatusColumn.Show();
			}
			else
			{
				ColumnsViewModel.GitCommitAuthorColumn.Hide();
				ColumnsViewModel.GitLastCommitDateColumn.Hide();
				ColumnsViewModel.GitLastCommitMessageColumn.Hide();
				ColumnsViewModel.GitLastCommitShaColumn.Hide();
				ColumnsViewModel.GitStatusColumn.Hide();
			}

			// Show path columns in git repository
			if (e.IsTypeSearchResults)
				ColumnsViewModel.PathColumn.Show();
			else
				ColumnsViewModel.PathColumn.Hide();
		}

		private IList<MenuFlyoutItemBase> GetColumnsHeaderContextMenuFlyout()
		{
			var contextMenuFlyoutItemModels = new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "SizeAllColumnsToFit".GetLocalizedResource(),
					Command = ResizeAllColumnsToFitCommand,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
				},
				new()
				{
					Text = "Git".GetLocalizedResource(),
					IsHidden = !InstanceViewModel.IsGitRepository,
					Items = new()
					{
						new()
						{
							ItemType = ContextMenuFlyoutItemType.Toggle,
							IsChecked = !ColumnsViewModel.GitStatusColumn.UserCollapsed,
							Text = "GitStatus".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitStatusColumn,
							IsHidden = ColumnsViewModel.GitStatusColumn.IsHidden,
						},
						new()
						{
							ItemType = ContextMenuFlyoutItemType.Toggle,
							IsChecked = !ColumnsViewModel.GitLastCommitDateColumn.UserCollapsed,
							Text = "DateCommitted".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitLastCommitDateColumn,
							IsHidden = ColumnsViewModel.GitLastCommitDateColumn.IsHidden,
						},
						new()
						{
							ItemType = ContextMenuFlyoutItemType.Toggle,
							IsChecked = !ColumnsViewModel.GitLastCommitMessageColumn.UserCollapsed,
							Text = "CommitMessage".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitLastCommitMessageColumn,
							IsHidden = ColumnsViewModel.GitLastCommitMessageColumn.IsHidden,
						},
						new()
						{
							ItemType = ContextMenuFlyoutItemType.Toggle,
							IsChecked = !ColumnsViewModel.GitCommitAuthorColumn.UserCollapsed,
							Text = "Author".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitCommitAuthorColumn,
							IsHidden = ColumnsViewModel.GitCommitAuthorColumn.IsHidden,
						},
						new()
						{
							ItemType = ContextMenuFlyoutItemType.Toggle,
							IsChecked = !ColumnsViewModel.GitLastCommitShaColumn.UserCollapsed,
							Text = "CommitSha".GetLocalizedResource(),
							Command = ToggleColumnCommand,
							CommandParameter = ColumnsViewModel.GitLastCommitShaColumn,
							IsHidden = ColumnsViewModel.GitLastCommitShaColumn.IsHidden,
						},
					}
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = !ColumnsViewModel.TagColumn.UserCollapsed,
					Text = "Tag".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.TagColumn,
					IsHidden = ColumnsViewModel.TagColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = !ColumnsViewModel.PathColumn.UserCollapsed,
					Text = "PathColumn".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.PathColumn,
					IsHidden = ColumnsViewModel.PathColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = !ColumnsViewModel.OriginalPathColumn.UserCollapsed,
					Text = "DetailsViewHeaderFlyout_ShowOriginalPath/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.OriginalPathColumn,
					IsHidden = ColumnsViewModel.OriginalPathColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = !ColumnsViewModel.DateDeletedColumn.UserCollapsed,
					Text = "DetailsViewHeaderFlyout_ShowDateDeleted/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.DateDeletedColumn,
					IsHidden = ColumnsViewModel.DateDeletedColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = !ColumnsViewModel.DateModifiedColumn.UserCollapsed,
					Text = "DetailsViewHeaderFlyout_ShowDateModified/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.DateModifiedColumn,
					IsHidden = ColumnsViewModel.DateModifiedColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = !ColumnsViewModel.DateCreatedColumn.UserCollapsed,
					Text = "DetailsViewHeaderFlyout_ShowDateCreated/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.DateCreatedColumn,
					IsHidden = ColumnsViewModel.DateCreatedColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = !ColumnsViewModel.ItemTypeColumn.UserCollapsed,
					Text = "DetailsViewHeaderFlyout_ShowItemType/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.ItemTypeColumn,
					IsHidden = ColumnsViewModel.ItemTypeColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = !ColumnsViewModel.SizeColumn.UserCollapsed,
					Text = "DetailsViewHeaderFlyout_ShowItemSize/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.SizeColumn,
					IsHidden = ColumnsViewModel.SizeColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Toggle,
					IsChecked = !ColumnsViewModel.StatusColumn.UserCollapsed,
					Text = "DetailsViewHeaderFlyout_ShowSyncStatus/Text".GetLocalizedResource(),
					Command = ToggleColumnCommand,
					CommandParameter = ColumnsViewModel.StatusColumn,
					IsHidden = ColumnsViewModel.StatusColumn.IsHidden,
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
				},
				new()
				{
					Text = "SetAsDefault".GetLocalizedResource(),
					Command = SetColumnsAsDefaultCommand,
				},
			};

			var list = Helpers.ContextFlyouts.ItemModelListToContextFlyoutHelper.GetMenuFlyoutItemsFromModel(contextMenuFlyoutItemModels);

			return list;
		}

		public IList<ColumnDefinition> GetColumnHeaderDefinitions()
		{
			var collection = new List<ColumnDefinition>()
			{
				new()
				{
					Width = ColumnsViewModel.IconColumn.Length,
					MaxWidth = ColumnsViewModel.IconColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.NameColumn.Length,
					MaxWidth = ColumnsViewModel.NameColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.GitStatusColumn.Length,
					MaxWidth = ColumnsViewModel.GitStatusColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.GitLastCommitDateColumn.Length,
					MaxWidth = ColumnsViewModel.GitLastCommitDateColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.GitLastCommitMessageColumn.Length,
					MaxWidth = ColumnsViewModel.GitLastCommitMessageColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.GitCommitAuthorColumn.Length,
					MaxWidth = ColumnsViewModel.GitCommitAuthorColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.GitLastCommitShaColumn.Length,
					MaxWidth = ColumnsViewModel.GitLastCommitShaColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.TagColumn.Length,
					MaxWidth = ColumnsViewModel.TagColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.PathColumn.Length,
					MaxWidth = ColumnsViewModel.PathColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.OriginalPathColumn.Length,
					MaxWidth = ColumnsViewModel.OriginalPathColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.DateDeletedColumn.Length,
					MaxWidth = ColumnsViewModel.DateDeletedColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.DateModifiedColumn.Length,
					MaxWidth = ColumnsViewModel.DateModifiedColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.DateCreatedColumn.Length,
					MaxWidth = ColumnsViewModel.DateCreatedColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.ItemTypeColumn.Length,
					MaxWidth = ColumnsViewModel.ItemTypeColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.SizeColumn.Length,
					MaxWidth = ColumnsViewModel.SizeColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
				new()
				{
					Width = ColumnsViewModel.StatusColumn.Length,
					MaxWidth = ColumnsViewModel.StatusColumn.MaxLength,
				},
				new()
				{
					Width = new GridLength(0, GridUnitType.Auto),
				},
			};

			return collection;
		}

		public IList<FrameworkElement> GetColumnHeaderItems()
		{
			var collection = new List<FrameworkElement>()
			{
				new DataGridHeader()
				{
					Margin = new(4, 0, -4, 0),
					Command = UpdateSortOptionsCommand,
					CommandParameter = "Name",
					Header = "Name".GetLocalizedResource(),
				},
				new GridSplitter()
				{
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "GitStatus",
					Header = "GitStatus".GetLocalizedResource(),
					Visibility = ColumnsViewModel.GitStatusColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.GitStatusColumn.Visibility
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "ItemLastCommitDate",
					Header = "DateCommitted".GetLocalizedResource(),
					Visibility = ColumnsViewModel.GitLastCommitDateColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.GitLastCommitDateColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "ItemLastCommitMessage",
					Header = "CommitMessage".GetLocalizedResource(),
					Visibility = ColumnsViewModel.GitLastCommitMessageColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.GitLastCommitMessageColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "ItemLastCommitAuthor",
					Header = "Author".GetLocalizedResource(),
					Visibility = ColumnsViewModel.GitCommitAuthorColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.GitCommitAuthorColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "ItemLastCommitSha",
					Header = "CommitSha".GetLocalizedResource(),
					Visibility = ColumnsViewModel.GitLastCommitShaColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.GitLastCommitShaColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "FileTag",
					Header = "Tag".GetLocalizedResource(),
					Visibility = ColumnsViewModel.TagColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.TagColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "Path",
					Header = "Path".GetLocalizedResource(),
					Visibility = ColumnsViewModel.PathColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.PathColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "OriginalFolder",
					Header = "OriginalPath".GetLocalizedResource(),
					Visibility = ColumnsViewModel.OriginalPathColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.OriginalPathColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "DateDeleted",
					Header = "DateDeleted".GetLocalizedResource(),
					Visibility = ColumnsViewModel.DateDeletedColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.DateDeletedColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "DateModified",
					Header = "DateModifiedLowerCase".GetLocalizedResource(),
					Visibility = ColumnsViewModel.DateModifiedColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.DateModifiedColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "DateCreated",
					Header = "DateCreated".GetLocalizedResource(),
					Visibility = ColumnsViewModel.DateCreatedColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.DateCreatedColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "FileType",
					Header = "Type".GetLocalizedResource(),
					Visibility = ColumnsViewModel.ItemTypeColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.ItemTypeColumn.Visibility,
				},
				new DataGridHeader()
				{
					Command = UpdateSortOptionsCommand,
					CommandParameter = "Size",
					Header = "Size".GetLocalizedResource(),
					Visibility = ColumnsViewModel.SizeColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.SizeColumn.Visibility,
				},
				new DataGridHeader()
				{
					CommandParameter = "SyncStatus",
					Command = UpdateSortOptionsCommand,
					Header = "syncStatusColumn/Header".GetLocalizedResource(),
					HorizontalAlignment=HorizontalAlignment.Stretch,
					HorizontalContentAlignment=HorizontalAlignment.Left,
					Visibility = ColumnsViewModel.StatusColumn.Visibility,
				},
				new GridSplitter()
				{
					Visibility = ColumnsViewModel.StatusColumn.Visibility,
				},
			};

			return collection;
		}

		private void SetGridColumnForColumnHeaders()
		{
			for (int index = 0; index < ColumnHeaderItems.Count; index++)
			{
				Grid.SetColumn(ColumnHeaderItems[index], index + 2);
			}
		}

		private void ToggleColumn(DetailsLayoutColumnItem? item)
		{
			// Toggle the column
			item.UserCollapsed = !item.UserCollapsed;

			// Update settings
			FolderSettings.ColumnsViewModel = ColumnsViewModel;
		}

		private void SetColumnsAsDefault()
		{
			FolderSettings.SetDefaultLayoutPreferences(ColumnsViewModel);
		}

		private void ResizeAllColumnsToFit()
		{
			// If there aren't items, do not make columns fit
			if (!ListedItems.Any())
				return;

			// For scalability, just count the # of public `ColumnViewModel` properties in ColumnsViewModel
			int totalColumnCount =
				ColumnsViewModel
					.GetType()
					.GetProperties()
					.Count(prop => prop.PropertyType == typeof(DetailsLayoutColumnItem));

			for (int columnIndex = 1; columnIndex <= totalColumnCount; columnIndex++)
				ResizeColumnToFit(columnIndex);
		}

		private void UpdateSortOptions(string? option)
		{
			if (!Enum.TryParse<SortOption>(option, out var val))
				return;

			if (FolderSettings.DirectorySortOption == val)
			{
				FolderSettings.DirectorySortDirection = (SortDirection)(((int)FolderSettings.DirectorySortDirection + 1) % 2);
			}
			else
			{
				FolderSettings.DirectorySortOption = val;
				FolderSettings.DirectorySortDirection = SortDirection.Ascending;
			}
		}

		public void UpdateColumnLayout()
		{
			ColumnsViewModel.IconColumn.UserLength = new(ColumnHeaderDefinitionItems[0].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.NameColumn.UserLength = new(ColumnHeaderDefinitionItems[2].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.GitStatusColumn.UserLength = new(ColumnHeaderDefinitionItems[4].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.GitLastCommitDateColumn.UserLength = new(ColumnHeaderDefinitionItems[6].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.GitLastCommitMessageColumn.UserLength = new(ColumnHeaderDefinitionItems[8].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.GitCommitAuthorColumn.UserLength = new(ColumnHeaderDefinitionItems[10].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.GitLastCommitShaColumn.UserLength = new(ColumnHeaderDefinitionItems[12].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.TagColumn.UserLength = new(ColumnHeaderDefinitionItems[14].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.PathColumn.UserLength = new(ColumnHeaderDefinitionItems[16].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.OriginalPathColumn.UserLength = new(ColumnHeaderDefinitionItems[18].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.DateDeletedColumn.UserLength = new(ColumnHeaderDefinitionItems[20].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.DateModifiedColumn.UserLength = new(ColumnHeaderDefinitionItems[22].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.DateCreatedColumn.UserLength = new(ColumnHeaderDefinitionItems[24].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.ItemTypeColumn.UserLength = new(ColumnHeaderDefinitionItems[26].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.SizeColumn.UserLength = new(ColumnHeaderDefinitionItems[28].ActualWidth, GridUnitType.Pixel);
			ColumnsViewModel.StatusColumn.UserLength = new(ColumnHeaderDefinitionItems[30].ActualWidth, GridUnitType.Pixel);
		}

		public void ResizeColumnToFit(int columnToResize)
		{
			if (!ListedItems.Any())
				return;

			// Get max item length that is requested to resize to fit
			var maxItemLength = columnToResize switch
			{
				// Item icon
				1 => 40,
				// Item name
				2 => ListedItems.Select(x => x.Name?.Length ?? 0).Max(),
				4 => ListedItems.Select(x => (x as GitItem)?.GitLastCommitDateHumanized?.Length ?? 0).Max(), // git
				5 => ListedItems.Select(x => (x as GitItem)?.GitLastCommitMessage?.Length ?? 0).Max(), // git
				6 => ListedItems.Select(x => (x as GitItem)?.GitLastCommitAuthor?.Length ?? 0).Max(), // git
				7 => ListedItems.Select(x => (x as GitItem)?.GitLastCommitSha?.Length ?? 0).Max(), // git
				8 => ListedItems.Select(x => x.FileTagsUI?.Sum(x => x?.Name?.Length ?? 0) ?? 0).Max(), // file tag column
				9 => ListedItems.Select(x => x.ItemPath?.Length ?? 0).Max(), // path column
				10 => ListedItems.Select(x => (x as RecycleBinItem)?.ItemOriginalPath?.Length ?? 0).Max(), // original path column
				11 => ListedItems.Select(x => (x as RecycleBinItem)?.ItemDateDeleted?.Length ?? 0).Max(), // date deleted column
				12 => ListedItems.Select(x => x.ItemDateModified?.Length ?? 0).Max(), // date modified column
				13 => ListedItems.Select(x => x.ItemDateCreated?.Length ?? 0).Max(), // date created column
				14 => ListedItems.Select(x => x.ItemType?.Length ?? 0).Max(), // item type column
				15 => ListedItems.Select(x => x.FileSize?.Length ?? 0).Max(), // item size column
				_ => 20 // cloud status column
			};

			// If called programmatically, the column could be hidden
			// In this case, resizing doesn't need to be done at all
			if (maxItemLength == 0)
				return;

			// Estimate columns size to fit judging from max length item
			var columnSizeToFit = MeasureColumnEstimate(columnToResize, 5, maxItemLength);

			if (columnSizeToFit > 1)
			{
				var column = columnToResize switch
				{
					2 => ColumnsViewModel.NameColumn,
					3 => ColumnsViewModel.GitStatusColumn,
					4 => ColumnsViewModel.GitLastCommitDateColumn,
					5 => ColumnsViewModel.GitLastCommitMessageColumn,
					6 => ColumnsViewModel.GitCommitAuthorColumn,
					7 => ColumnsViewModel.GitLastCommitShaColumn,
					8 => ColumnsViewModel.TagColumn,
					9 => ColumnsViewModel.PathColumn,
					10 => ColumnsViewModel.OriginalPathColumn,
					11 => ColumnsViewModel.DateDeletedColumn,
					12 => ColumnsViewModel.DateModifiedColumn,
					13 => ColumnsViewModel.DateCreatedColumn,
					14 => ColumnsViewModel.ItemTypeColumn,
					15 => ColumnsViewModel.SizeColumn,
					_ => ColumnsViewModel.StatusColumn
				};

				// Overestimate
				if (columnToResize == 2) // file name column
					columnSizeToFit += 20;

				var minFitLength = Math.Max(columnSizeToFit, column.NormalMinLength);
				var maxFitLength = Math.Min(minFitLength + 36, column.NormalMaxLength); // 36 to account for SortIcon & padding

				// Set size
				column.UserLength = new GridLength(maxFitLength, GridUnitType.Pixel);
			}

			FolderSettings.ColumnsViewModel = ColumnsViewModel;
		}

		private double MeasureColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
		{
			if (columnIndex == 15) // sync status
				return maxItemLength;

			if (columnIndex == 8) // file tag
				return MeasureTagColumnEstimate(columnIndex);

			return MeasureTextColumnEstimate(columnIndex, measureItemsCount, maxItemLength);
		}

		private double MeasureTagColumnEstimate(int columnIndex)
		{
			var grids = DependencyObjectHelpers
				.FindChildren<Grid>(ListViewBase.ItemsPanelRoot)
				.Where(grid => IsCorrectColumn(grid, columnIndex));

			// Get the list of stack panels with the most letters
			var stackPanels = grids
				.Select(DependencyObjectHelpers.FindChildren<StackPanel>)
				.OrderByDescending(sps => sps.Select(sp => DependencyObjectHelpers.FindChildren<TextBlock>(sp).Select(tb => tb.Text.Length).Sum()).Sum())
				.First()
				.ToArray();

			var mesuredSize = stackPanels.Select(x =>
			{
				x.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

				return x.DesiredSize.Width;
			}).Sum();

			if (stackPanels.Length >= 2)
				mesuredSize += 4 * (stackPanels.Length - 1); // The spacing between the tags

			return mesuredSize;
		}

		private double MeasureTextColumnEstimate(int columnIndex, int measureItemsCount, int maxItemLength)
		{
			var tbs = DependencyObjectHelpers
				.FindChildren<TextBlock>(ListViewBase.ItemsPanelRoot)
				.Where(tb => IsCorrectColumn(tb, columnIndex));

			// heuristic: usually, text with more letters are wider than shorter text with wider letters
			// with this, we can calculate avg width using longest text(s) to avoid overshooting the width
			var widthPerLetter = tbs
				.OrderByDescending(x => x.Text.Length)
				.Where(tb => !string.IsNullOrEmpty(tb.Text))
				.Take(measureItemsCount)
				.Select(tb =>
				{
					var sampleTb = new TextBlock { Text = tb.Text, FontSize = tb.FontSize, FontFamily = tb.FontFamily };
					sampleTb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));

					return sampleTb.DesiredSize.Width / Math.Max(1, tb.Text.Length);
				});

			if (!widthPerLetter.Any())
				return 0;

			// Take weighted avg between mean and max since width is an estimate
			var weightedAvg = (widthPerLetter.Average() + widthPerLetter.Max()) / 2;
			return weightedAvg * maxItemLength;
		}

		public void UpdateSortIndicator()
		{
			ColumnHeaderItems[0].TryCast<DataGridHeader>()!.ColumnSortOption =  FolderSettings.DirectorySortOption == SortOption.Name ? FolderSettings.DirectorySortDirection : null;
			ColumnHeaderItems[12].TryCast<DataGridHeader>()!.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.FileTag ? FolderSettings.DirectorySortDirection : null;
			ColumnHeaderItems[14].TryCast<DataGridHeader>()!.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.Path ? FolderSettings.DirectorySortDirection : null;
			ColumnHeaderItems[16].TryCast<DataGridHeader>()!.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.OriginalFolder ? FolderSettings.DirectorySortDirection : null;
			ColumnHeaderItems[18].TryCast<DataGridHeader>()!.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateDeleted ? FolderSettings.DirectorySortDirection : null;
			ColumnHeaderItems[20].TryCast<DataGridHeader>()!.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateModified ? FolderSettings.DirectorySortDirection : null;
			ColumnHeaderItems[22].TryCast<DataGridHeader>()!.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.DateCreated ? FolderSettings.DirectorySortDirection : null;
			ColumnHeaderItems[24].TryCast<DataGridHeader>()!.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.FileType ? FolderSettings.DirectorySortDirection : null;
			ColumnHeaderItems[26].TryCast<DataGridHeader>()!.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.Size ? FolderSettings.DirectorySortDirection : null;
			ColumnHeaderItems[28].TryCast<DataGridHeader>()!.ColumnSortOption = FolderSettings.DirectorySortOption == SortOption.SyncStatus ? FolderSettings.DirectorySortDirection : null;
		}

		private bool IsCorrectColumn(FrameworkElement element, int columnIndex)
		{
			int columnIndexFromName = element.Name switch
			{
				"ItemName" => 2,
				"ItemGitStatusTextBlock" => 3,
				"ItemGitLastCommitDateTextBlock" => 4,
				"ItemGitLastCommitMessageTextBlock" => 5,
				"ItemGitCommitAuthorTextBlock" => 6,
				"ItemGitLastCommitShaTextBlock" => 7,
				"ItemTagGrid" => 8,
				"ItemPath" => 9,
				"ItemOriginalPath" => 10,
				"ItemDateDeleted" => 11,
				"ItemDateModified" => 12,
				"ItemDateCreated" => 13,
				"ItemType" => 14,
				"ItemSize" => 15,
				"ItemStatus" => 16,
				_ => -1,
			};

			return columnIndexFromName != -1 && columnIndexFromName == columnIndex;
		}
	}
}
