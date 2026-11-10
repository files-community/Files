// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Settings
{
	public sealed partial class LayoutViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// FileTag combobox indexes (required to hide SyncStatus)
		private readonly int FileTagSortingIndex = 5;
		private readonly int FileTagGroupingIndex = 6;

		// The Windows property columns follow the built-in options in the comboboxes
		private readonly int PropertySortingStartIndex = 6;
		private readonly int PropertyGroupingStartIndex = 7;

		private IReadOnlyDictionary<string, ShellPropertyColumnInfo>? _shellPropertyCatalog;

		public LayoutViewModel()
		{
			// Layout mode
			SelectedDefaultLayoutModeIndex = (int)DefaultLayoutMode;

			// Sorting options
			SelectedDefaultSortingIndex = GetSortingIndex(UserSettingsService.LayoutSettingsService);
			SelectedDefaultSortPriorityIndex = UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles ? 2 : UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst ? 1 : 0;

			// Grouping options
			SelectedDefaultGroupingIndex = GetGroupingIndex(UserSettingsService.LayoutSettingsService);
			SelectedDefaultGroupByDateUnitIndex = (int)UserSettingsService.LayoutSettingsService.DefaultGroupByDateUnit;
		}

		internal void StartListening()
		{
			UserSettingsService.OnSettingChangedEvent -= OnSharedSettingChanged;
			UserSettingsService.OnSettingChangedEvent += OnSharedSettingChanged;
			RefreshSettings();
		}

		internal void StopListening() => UserSettingsService.OnSettingChangedEvent -= OnSharedSettingChanged;

		private void OnSharedSettingChanged(object? sender, SettingChangedEventArgs e)
		{
			if (e.NewValue is JsonElement)
				RefreshSettings(e.SettingName);
		}

		private void RefreshSettings(string? name = null)
		{
			var settings = UserSettingsService.LayoutSettingsService;
			if (name is null or nameof(ILayoutSettingsService.DefaultLayoutMode))
			{
				SetProperty(ref selectedDefaultLayoutModeIndex, (int)settings.DefaultLayoutMode, nameof(SelectedDefaultLayoutModeIndex));
			}
			if (name is null or nameof(ILayoutSettingsService.DefaultSortOption) or nameof(ILayoutSettingsService.DefaultSortColumnId))
			{
				SetProperty(ref selectedDefaultSortingIndex, GetSortingIndex(settings), nameof(SelectedDefaultSortingIndex));
			}
			if (name is null or nameof(ILayoutSettingsService.DefaultSortDirectoriesAlongsideFiles) or nameof(ILayoutSettingsService.DefaultSortFilesFirst))
			{
				SetProperty(ref selectedDefaultSortPriorityIndex, settings.DefaultSortDirectoriesAlongsideFiles ? 2 : settings.DefaultSortFilesFirst ? 1 : 0, nameof(SelectedDefaultSortPriorityIndex));
			}
			if (name is null or nameof(ILayoutSettingsService.DefaultGroupOption) or nameof(ILayoutSettingsService.DefaultGroupColumnId))
			{
				SetProperty(ref selectedDefaultGroupingIndex, GetGroupingIndex(settings), nameof(SelectedDefaultGroupingIndex));
				OnPropertyChanged(nameof(IsDefaultGrouped));
				OnPropertyChanged(nameof(IsGroupByDate));
			}
			if (name is null or nameof(ILayoutSettingsService.DefaultGroupByDateUnit))
			{
				SetProperty(ref defaultGroupByDateUnitIndex, (int)settings.DefaultGroupByDateUnit, nameof(SelectedDefaultGroupByDateUnitIndex));
			}
			if (name is null or nameof(ILayoutSettingsService.DefaultDirectorySortDirection))
			{
				OnPropertyChanged(nameof(SortInDescendingOrder));
			}
			if (name is null or nameof(ILayoutSettingsService.DefaultDirectoryGroupDirection))
			{
				OnPropertyChanged(nameof(GroupInDescendingOrder));
			}
			OnPropertyChanged(name ?? string.Empty);
		}

		// Properties

		public bool SyncFolderPreferencesAcrossDirectories
		{
			get => UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories)
				{
					UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories = value;

					ResetLayoutPreferences();
					OnPropertyChanged();

					if (value && DefaultLayoutMode is FolderLayoutModes.Adaptive)
					{
						// Change the default layout to Details, as Adaptive layout is not available when preferences are synced.
						SelectedDefaultLayoutModeIndex = 0;
						ShowAdaptiveDisabledTeachingTip = true;
					}
				}
			}
		}

		// Layout mode

		public FolderLayoutModes DefaultLayoutMode
		{
			get => UserSettingsService.LayoutSettingsService.DefaultLayoutMode;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.DefaultLayoutMode)
				{
					UserSettingsService.LayoutSettingsService.DefaultLayoutMode = value;

					OnPropertyChanged();
				}
			}
		}

		private int selectedDefaultLayoutModeIndex;
		public int SelectedDefaultLayoutModeIndex
		{
			get => selectedDefaultLayoutModeIndex;
			set
			{
				if (SetProperty(ref selectedDefaultLayoutModeIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultLayoutModeIndex));
					DefaultLayoutMode = (FolderLayoutModes)value;
				}
			}
		}


		// Sorting options

		private int selectedDefaultSortingIndex;
		public int SelectedDefaultSortingIndex
		{
			get => selectedDefaultSortingIndex;
			set
			{
				if (SetProperty(ref selectedDefaultSortingIndex, value) && value >= 0)
				{
					OnPropertyChanged(nameof(SelectedDefaultSortingIndex));

					if (value >= PropertySortingStartIndex)
					{
						UserSettingsService.LayoutSettingsService.DefaultSortColumnId = SortColumnOptions[value - PropertySortingStartIndex].Id;
						UserSettingsService.LayoutSettingsService.DefaultSortOption = SortOption.Property;
					}
					else
					{
						UserSettingsService.LayoutSettingsService.DefaultSortOption = value == FileTagSortingIndex ? SortOption.FileTag : (SortOption)value;
					}
				}
			}
		}

		public bool SortInDescendingOrder
		{
			get => UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection == SortDirection.Descending;
			set
			{
				if (value != (UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection == SortDirection.Descending))
				{
					UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection = value ? SortDirection.Descending : SortDirection.Ascending;
					OnPropertyChanged();
				}
			}
		}


		private int selectedDefaultSortPriorityIndex;
		public int SelectedDefaultSortPriorityIndex
		{
			get => selectedDefaultSortPriorityIndex;
			set
			{
				if (SetProperty(ref selectedDefaultSortPriorityIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultSortPriorityIndex));

					switch (value)
					{
						case 0:
							UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = false;
							UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst = false;
							break;
						case 1:
							UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = false;
							UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst = true;
							break;
						case 2:
							UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = true;
							break;
						default:
							break;
					}
				}
			}
		}


		// Grouping options

		private int selectedDefaultGroupingIndex;
		public int SelectedDefaultGroupingIndex
		{
			get => selectedDefaultGroupingIndex;
			set
			{
				if (SetProperty(ref selectedDefaultGroupingIndex, value) && value >= 0)
				{
					OnPropertyChanged(nameof(SelectedDefaultGroupingIndex));

					if (value >= PropertyGroupingStartIndex)
					{
						UserSettingsService.LayoutSettingsService.DefaultGroupColumnId = GroupColumnOptions[value - PropertyGroupingStartIndex].Id;
						UserSettingsService.LayoutSettingsService.DefaultGroupOption = GroupOption.Property;
					}
					else
					{
						UserSettingsService.LayoutSettingsService.DefaultGroupOption = value == FileTagGroupingIndex ? GroupOption.FileTag : (GroupOption)value;
					}

					// Raise an event for the grouping option toggle switches availability
					OnPropertyChanged(nameof(IsDefaultGrouped));
					OnPropertyChanged(nameof(IsGroupByDate));
				}
			}
		}

		public bool GroupInDescendingOrder
		{
			get => UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection == SortDirection.Descending;
			set
			{
				if (value != (UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection == SortDirection.Descending))
				{
					UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection = value ? SortDirection.Descending : SortDirection.Ascending;
					OnPropertyChanged();
				}
			}
		}

		private int defaultGroupByDateUnitIndex;

		public int SelectedDefaultGroupByDateUnitIndex
		{
			get => defaultGroupByDateUnitIndex;
			set
			{
				if (SetProperty(ref defaultGroupByDateUnitIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultGroupByDateUnitIndex));
					UserSettingsService.LayoutSettingsService.DefaultGroupByDateUnit = (GroupByDateUnit)value;
				}
			}
		}

		public bool IsGroupByDate
			=> UserSettingsService.LayoutSettingsService.DefaultGroupOption.IsGroupByDate() ||
				UserSettingsService.LayoutSettingsService.DefaultGroupOption == GroupOption.Property && IsDateColumn(UserSettingsService.LayoutSettingsService.DefaultGroupColumnId);

		public bool IsDefaultGrouped
			=> UserSettingsService.LayoutSettingsService.DefaultGroupOption != GroupOption.None;


		// Windows property columns

		public IReadOnlyList<ShellFolderColumn> ColumnOptions { get; private set; } = [];

		public IReadOnlyList<ShellFolderColumn> SortColumnOptions => ColumnOptions;

		public IReadOnlyList<ShellFolderColumn> GroupColumnOptions { get; private set; } = [];

		internal async Task LoadColumnOptionsAsync()
		{
			_shellPropertyCatalog = await ShellPropertyCatalog.GetAsync();

			var columns = await ShellFolderColumns.GetAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
			ColumnOptions = columns.Where(column => ColumnValueHelper.IsShellPropertyColumn(column.Id)).ToArray();
			GroupColumnOptions = ColumnOptions.Where(column => column.CanGroup).ToArray();
		}

		public string GetColumnLabel(string id)
			=> DetailsLayoutColumnDefaults.GetDisplayName(id, _shellPropertyCatalog);

		public bool IsDefaultColumn(string id)
			=> UserSettingsService.LayoutSettingsService.DefaultPropertyColumns?.Contains(id) is true;

		public void SetDefaultColumn(string id, bool isShown)
		{
			var columns = UserSettingsService.LayoutSettingsService.DefaultPropertyColumns ?? [];
			if (isShown == columns.Contains(id))
				return;

			UserSettingsService.LayoutSettingsService.DefaultPropertyColumns = isShown ? [.. columns, id] : [.. columns.Where(column => column != id)];
		}

		private int GetSortingIndex(ILayoutSettingsService settings)
		{
			return settings.DefaultSortOption switch
			{
				SortOption.FileTag => FileTagSortingIndex,
				SortOption.Property => IndexOfColumn(SortColumnOptions, settings.DefaultSortColumnId, PropertySortingStartIndex),
				var option => (int)option,
			};
		}

		private int GetGroupingIndex(ILayoutSettingsService settings)
		{
			return settings.DefaultGroupOption switch
			{
				GroupOption.FileTag => FileTagGroupingIndex,
				GroupOption.Property => IndexOfColumn(GroupColumnOptions, settings.DefaultGroupColumnId, PropertyGroupingStartIndex),
				var option => (int)option,
			};
		}

		private static int IndexOfColumn(IReadOnlyList<ShellFolderColumn> columns, string id, int startIndex)
		{
			for (int i = 0; i < columns.Count; i++)
			{
				if (columns[i].Id == id)
					return startIndex + i;
			}

			return -1;
		}

		private bool IsDateColumn(string id)
			=> id == DetailsLayoutColumnIds.GitLastCommitDate || _shellPropertyCatalog is not null && _shellPropertyCatalog.TryGetValue(id, out var info) && info.IsDate;


		// Details view

		public bool AutoSizeColumnsInDetailsLayout
		{
			get => UserSettingsService.LayoutSettingsService.AutoSizeColumnsInDetailsLayout;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.AutoSizeColumnsInDetailsLayout)
				{
					UserSettingsService.LayoutSettingsService.AutoSizeColumnsInDetailsLayout = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowFileTagColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowFileTagColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowFileTagColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowFileTagColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowSizeColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowSizeColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowSizeColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowSizeColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowTypeColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowTypeColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowTypeColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowTypeColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateCreatedColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowDateColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowDateColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowDateColumn = value;

					OnPropertyChanged();
				}
			}
		}

		private bool _ShowAdaptiveDisabledTeachingTip;
		public bool ShowAdaptiveDisabledTeachingTip
		{
			get => _ShowAdaptiveDisabledTeachingTip;
			set => SetProperty(ref _ShowAdaptiveDisabledTeachingTip, value);
		}


		// Methods

		public void ResetLayoutPreferences()
		{
			// Is this proper practice?
			var dbInstance = LayoutPreferencesManager.GetDatabaseManagerInstance();

			dbInstance.ResetAll();
		}
	}
}
