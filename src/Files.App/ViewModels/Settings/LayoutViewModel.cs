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

		public LayoutViewModel()
		{
			// Layout mode
			SelectedDefaultLayoutModeIndex = (int)DefaultLayoutMode;

			// Sorting options
			SelectedDefaultSortingIndex = UserSettingsService.LayoutSettingsService.DefaultSortOption switch
			{
				SortOption.FileTag => FileTagSortingIndex,
				SortOption.Column => -1,
				var option => (int)option,
			};
			SelectedDefaultSortPriorityIndex = UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles ? 2 : UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst ? 1 : 0;

			// Grouping options
			SelectedDefaultGroupingIndex = UserSettingsService.LayoutSettingsService.DefaultGroupOption switch
			{
				GroupOption.FileTag => FileTagGroupingIndex,
				GroupOption.Column => -1,
				var option => (int)option,
			};
			SelectedDefaultGroupByDateUnitIndex = (int)UserSettingsService.LayoutSettingsService.DefaultGroupByDateUnit;

			LoadDefaultDetailsColumns();
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
			if (name is null or nameof(ILayoutSettingsService.DefaultSortOption))
			{
				SetProperty(ref selectedDefaultSortingIndex, settings.DefaultSortOption == SortOption.FileTag ? FileTagSortingIndex : (int)settings.DefaultSortOption, nameof(SelectedDefaultSortingIndex));
			}
			if (name is null or nameof(ILayoutSettingsService.DefaultSortDirectoriesAlongsideFiles) or nameof(ILayoutSettingsService.DefaultSortFilesFirst))
			{
				SetProperty(ref selectedDefaultSortPriorityIndex, settings.DefaultSortDirectoriesAlongsideFiles ? 2 : settings.DefaultSortFilesFirst ? 1 : 0, nameof(SelectedDefaultSortPriorityIndex));
			}
			if (name is null or nameof(ILayoutSettingsService.DefaultGroupOption))
			{
				SetProperty(ref selectedDefaultGroupingIndex, settings.DefaultGroupOption == GroupOption.FileTag ? FileTagGroupingIndex : (int)settings.DefaultGroupOption, nameof(SelectedDefaultGroupingIndex));
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

					UserSettingsService.LayoutSettingsService.DefaultSortOption = value == FileTagSortingIndex ? SortOption.FileTag : (SortOption)value;
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

					UserSettingsService.LayoutSettingsService.DefaultGroupOption = value == FileTagGroupingIndex ? GroupOption.FileTag : (GroupOption)value;

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
			=> UserSettingsService.LayoutSettingsService.DefaultGroupOption.IsGroupByDate();

		public bool IsDefaultGrouped
			=> UserSettingsService.LayoutSettingsService.DefaultGroupOption != GroupOption.None;


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

		public ObservableCollection<DetailsLayoutColumnSettingItem> DefaultDetailsColumns { get; } = [];

		private void LoadDefaultDetailsColumns()
		{
			foreach (var id in DetailsLayoutColumnDefaults.GetColumns())
				DefaultDetailsColumns.Add(new(id, GetColumnDisplayName(id, null)));

			DefaultDetailsColumns.CollectionChanged += (_, _) => DetailsLayoutColumnDefaults.SetColumns(DefaultDetailsColumns.Select(column => column.Id));
			_ = UpdateDefaultDetailsColumnNamesAsync();
		}

		private async Task UpdateDefaultDetailsColumnNamesAsync()
		{
			var catalog = await ShellPropertyCatalog.GetAsync();
			foreach (var column in DefaultDetailsColumns)
				column.DisplayName = GetColumnDisplayName(column.Id, catalog);
		}

		public static string GetColumnDisplayName(string id, IReadOnlyDictionary<string, ShellPropertyColumnInfo>? catalog)
		{
			if (!ColumnValueHelper.IsShellPropertyColumn(id))
				return DetailsLayoutColumnDefaults.GetBuiltInDisplayName(id);

			return catalog is not null && catalog.TryGetValue(id, out var info) ? info.DisplayName : id;
		}

		public bool HasDefaultDetailsColumn(string id)
		{
			return DefaultDetailsColumns.Any(column => column.Id == id);
		}

		public void AddDefaultDetailsColumn(string id, string displayName)
		{
			if (!HasDefaultDetailsColumn(id))
				DefaultDetailsColumns.Add(new(id, displayName));
		}

		public void RemoveDefaultDetailsColumn(DetailsLayoutColumnSettingItem column)
		{
			if (column.CanRemove)
				DefaultDetailsColumns.Remove(column);
		}

		public void MoveDefaultDetailsColumn(DetailsLayoutColumnSettingItem column, int offset)
		{
			var index = DefaultDetailsColumns.IndexOf(column);
			var newIndex = index + offset;
			if (index >= 0 && newIndex >= 0 && newIndex < DefaultDetailsColumns.Count)
				DefaultDetailsColumns.Move(index, newIndex);
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
