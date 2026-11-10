// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents manager for layout preferences settings.
	/// </summary>
	public sealed partial class LayoutPreferencesManager : ObservableObject
	{
		// Dependency injections

		private static ILayoutTemplatesService LayoutTemplatesService { get; } = Ioc.Default.GetRequiredService<ILayoutTemplatesService>();

		// Fields

		private static readonly Lazy<LayoutPreferencesDatabaseManager> _databaseInstance =
			new(() => new LayoutPreferencesDatabaseManager());

		private readonly FolderLayoutModes? _rootLayoutMode;

		private bool _isApplyingPreferences;

		// Properties
		public bool IsLayoutModeFixed
			=> _rootLayoutMode is not null;

		/// <summary>
		/// Gets the id of the layout template the current preferences were created from.
		/// </summary>
		public string TemplateId
			=> LayoutPreferencesItem.TemplateId;

		/// <summary>
		/// Gets whether the folder has its own preferences instead of following its template.
		/// </summary>
		public bool IsCustomized
			=> !LayoutPreferencesItem.FollowsTemplate;

		public FolderLayoutModes LayoutMode
		{
			get => _rootLayoutMode ?? LayoutPreferencesItem.LayoutMode;
			set
			{
				if (SetProperty(item => item.LayoutMode, item => item.LayoutMode = value, nameof(LayoutMode)))
					RequestSave();
			}
		}

		public SortOption DirectorySortOption
		{
			get => LayoutPreferencesItem.DirectorySortOption;
			set
			{
				if (SetProperty(item => item.DirectorySortOption, item => item.DirectorySortOption = value, nameof(DirectorySortOption)))
				{
					RequestSave();
					SortOptionPreferenceUpdated?.Invoke(this, DirectorySortOption);
				}
			}
		}

		public string DirectorySortPropertyId
			=> LayoutPreferencesItem.DirectorySortPropertyId;

		public void SetSortProperty(string columnId)
		{
			if (DirectorySortOption == SortOption.Property && DirectorySortPropertyId == columnId)
				return;

			LayoutPreferencesItem.DirectorySortPropertyId = columnId;
			LayoutPreferencesItem.DirectorySortOption = SortOption.Property;
			OnPropertyChanged(nameof(DirectorySortPropertyId));
			OnPropertyChanged(nameof(DirectorySortOption));
			RequestSave();
			SortOptionPreferenceUpdated?.Invoke(this, SortOption.Property);
		}

		public GroupOption DirectoryGroupOption
		{
			get => LayoutPreferencesItem.DirectoryGroupOption;
			set
			{
				if (SetProperty(item => item.DirectoryGroupOption, item => item.DirectoryGroupOption = value, nameof(DirectoryGroupOption)))
				{
					RequestSave();
					GroupOptionPreferenceUpdated?.Invoke(this, DirectoryGroupOption);
				}
			}
		}

		public string DirectoryGroupPropertyId
			=> LayoutPreferencesItem.DirectoryGroupPropertyId;

		public void SetGroupProperty(string columnId)
		{
			if (DirectoryGroupOption == GroupOption.Property && DirectoryGroupPropertyId == columnId)
				return;

			LayoutPreferencesItem.DirectoryGroupPropertyId = columnId;
			LayoutPreferencesItem.DirectoryGroupOption = GroupOption.Property;
			OnPropertyChanged(nameof(DirectoryGroupPropertyId));
			OnPropertyChanged(nameof(DirectoryGroupOption));
			RequestSave();
			GroupOptionPreferenceUpdated?.Invoke(this, GroupOption.Property);
		}

		public SortDirection DirectorySortDirection
		{
			get => LayoutPreferencesItem.DirectorySortDirection;
			set
			{
				if (SetProperty(item => item.DirectorySortDirection, item => item.DirectorySortDirection = value, nameof(DirectorySortDirection)))
				{
					RequestSave();
					SortDirectionPreferenceUpdated?.Invoke(this, DirectorySortDirection);
				}
			}
		}

		public SortDirection DirectoryGroupDirection
		{
			get => LayoutPreferencesItem.DirectoryGroupDirection;
			set
			{
				if (SetProperty(item => item.DirectoryGroupDirection, item => item.DirectoryGroupDirection = value, nameof(DirectoryGroupDirection)))
				{
					RequestSave();
					GroupDirectionPreferenceUpdated?.Invoke(this, DirectoryGroupDirection);
				}
			}
		}

		public GroupByDateUnit DirectoryGroupByDateUnit
		{
			get => LayoutPreferencesItem.DirectoryGroupByDateUnit;
			set
			{
				if (SetProperty(item => item.DirectoryGroupByDateUnit, item => item.DirectoryGroupByDateUnit = value, nameof(DirectoryGroupByDateUnit)))
				{
					RequestSave();
					GroupByDateUnitPreferenceUpdated?.Invoke(this, DirectoryGroupByDateUnit);
				}
			}
		}

		public bool SortDirectoriesAlongsideFiles
		{
			get => LayoutPreferencesItem.SortDirectoriesAlongsideFiles;
			set
			{
				if (SetProperty(item => item.SortDirectoriesAlongsideFiles, item => item.SortDirectoriesAlongsideFiles = value, nameof(SortDirectoriesAlongsideFiles)))
				{
					RequestSave();
					SortDirectoriesAlongsideFilesPreferenceUpdated?.Invoke(this, SortDirectoriesAlongsideFiles);
				}
			}
		}

		public bool SortFilesFirst
		{
			get => LayoutPreferencesItem.SortFilesFirst;
			set
			{
				if (SetProperty(item => item.SortFilesFirst, item => item.SortFilesFirst = value, nameof(SortFilesFirst)))
				{
					RequestSave();
					SortFilesFirstPreferenceUpdated?.Invoke(this, SortFilesFirst);
				}
			}
		}

		public ColumnsViewModel ColumnsViewModel
		{
			get => LayoutPreferencesItem.ColumnsViewModel;
			set
			{
				SetProperty(item => item.ColumnsViewModel, item => item.ColumnsViewModel = value, nameof(ColumnsViewModel));
				RequestSave();
			}
		}

		/// <summary>
		/// Saves new column widths without making the folder stop following its template.
		/// </summary>
		public void SaveColumnWidths(ColumnsViewModel columns)
		{
			SetProperty(item => item.ColumnsViewModel, item => item.ColumnsViewModel = columns, nameof(ColumnsViewModel));
			RequestSave(keepsTemplate: true);
		}

		private bool _IsLayoutModeChanging;
		public bool IsLayoutModeChanging
		{
			get => _IsLayoutModeChanging;
			set => SetProperty(ref _IsLayoutModeChanging, value);
		}

		// One-shot selection carried across a layout-mode change; kept off NavigationArguments so the back stack can't replay it
		public List<string>? PendingLayoutSwitchSelection { get; set; }

		private LayoutPreferencesItem? _LayoutPreferencesItem;
		public LayoutPreferencesItem LayoutPreferencesItem
		{
			get => _LayoutPreferencesItem!;
			private set
			{
				if (SetProperty(ref _LayoutPreferencesItem, value))
				{
					OnPropertyChanged(nameof(LayoutMode));
					OnPropertyChanged(nameof(TemplateId));
					OnPropertyChanged(nameof(IsCustomized));
					OnPropertyChanged(nameof(DirectoryGroupOption));
					OnPropertyChanged(nameof(DirectorySortOption));
					OnPropertyChanged(nameof(DirectorySortDirection));
					OnPropertyChanged(nameof(DirectoryGroupDirection));
					OnPropertyChanged(nameof(DirectoryGroupByDateUnit));
					OnPropertyChanged(nameof(SortDirectoriesAlongsideFiles));
					OnPropertyChanged(nameof(SortFilesFirst));
					OnPropertyChanged(nameof(ColumnsViewModel));
				}
			}
		}

		// Events

		public event EventHandler<LayoutPreferenceEventArgs>? LayoutPreferencesUpdateRequired;
		public event EventHandler<SortOption>? SortOptionPreferenceUpdated;
		public event EventHandler<GroupOption>? GroupOptionPreferenceUpdated;
		public event EventHandler<SortDirection>? SortDirectionPreferenceUpdated;
		public event EventHandler<SortDirection>? GroupDirectionPreferenceUpdated;
		public event EventHandler<GroupByDateUnit>? GroupByDateUnitPreferenceUpdated;
		public event EventHandler<bool>? SortDirectoriesAlongsideFilesPreferenceUpdated;
		public event EventHandler<bool>? SortFilesFirstPreferenceUpdated;
		public event EventHandler<LayoutModeEventArgs>? LayoutModeChangeRequested;

		// Constructors

		public LayoutPreferencesManager()
		{
			LayoutPreferencesItem = new LayoutPreferencesItem();
		}

		public LayoutPreferencesManager(FolderLayoutModes modeOverride) : this()
		{
			_rootLayoutMode = modeOverride;
		}

		// Methods

		public Type GetLayoutType(string? path, bool changeLayoutMode = true)
		{
			var preferencesItem = GetLayoutPreferencesForPath(path, () => FolderContentHelpers.ReadContent(path));
			if (preferencesItem is null)
				return typeof(DetailsLayoutPage);

			if (changeLayoutMode)
			{
				IsLayoutModeChanging = LayoutPreferencesItem.LayoutMode != preferencesItem.LayoutMode;
				LayoutPreferencesItem = preferencesItem;
				OnPropertyChanged(nameof(TemplateId));
				OnPropertyChanged(nameof(IsCustomized));
			}

			return GetLayoutPageType(preferencesItem.LayoutMode);
		}

		/// <summary>
		/// Gets the page type of the current layout, without reading the saved preferences again.
		/// </summary>
		public Type GetCurrentLayoutType()
			=> GetLayoutPageType(LayoutMode);

		private static Type GetLayoutPageType(FolderLayoutModes layoutMode)
		{
			return layoutMode switch
			{
				FolderLayoutModes.DetailsView => typeof(DetailsLayoutPage),
				FolderLayoutModes.ListView => typeof(GridLayoutPage),
				FolderLayoutModes.CardsView => typeof(GridLayoutPage),
				FolderLayoutModes.GridView => typeof(GridLayoutPage),
				FolderLayoutModes.ColumnView => typeof(ColumnsLayoutPage),
				_ => typeof(DetailsLayoutPage)
			};
		}

		/// <summary>
		/// Reloads the saved preferences of the specified folder.
		/// </summary>
		/// <param name="getContent">Gets the items of the folder for content rules.</param>
		public void ReloadPreferences(string? path, Func<LayoutFolderContent?>? getContent = null)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;

			var preferencesItem = GetLayoutPreferencesForPath(path, getContent);
			if (preferencesItem is not null && (preferencesItem.TemplateId != TemplateId || preferencesItem.FollowsTemplate != LayoutPreferencesItem.FollowsTemplate || !preferencesItem.Equals(LayoutPreferencesItem)))
				ApplyPreferences(preferencesItem);
		}

		/// <summary>
		/// Checks the content rules again once the items of the folder are loaded.
		/// </summary>
		public void ApplyContentRules(string? path, IList<ListedItem> items)
		{
			var trimmedPath = path.TrimPath();
			if (IsLayoutModeFixed || !LayoutTemplatesService.HasRules || string.IsNullOrEmpty(trimmedPath) || !IsPathUsingDefaultLayout(trimmedPath))
				return;

			var template = LayoutTemplatesService.GetTemplateForPath(trimmedPath, () => FolderContentHelpers.GetContent(items));
			if (template.Id == TemplateId)
				return;

			var preferences = LayoutPreferencesItem.FromTemplate(template);
			preferences.ColumnsViewModel.ApplyWidths(ColumnsViewModel);
			ApplyPreferences(preferences);
		}

		public bool IsPathUsingDefaultLayout(string? path)
		{
			return string.IsNullOrEmpty(path) ||
				GetLayoutPreferencesFromDatabase(path, Win32Helper.GetFolderFRN(path)) is null or { FollowsTemplate: true, TemplateId.Length: 0 };
		}

		/// <summary>
		/// Saves the current columns to the layout template of the folder.
		/// </summary>
		public void SaveColumnsToTemplate()
		{
			if (LayoutTemplatesService.GetTemplate(TemplateId)?.Clone() is not { } template)
				return;

			DetailsLayoutColumnDefaults.Save(ColumnsViewModel, template);
			LayoutTemplatesService.SaveTemplate(template);
		}

		public void ToggleLayoutModeColumnView()
			=> ToggleLayoutMode(FolderLayoutModes.ColumnView);

		public void ToggleLayoutModeGridView()
			=> ToggleLayoutMode(FolderLayoutModes.GridView);

		public void ToggleLayoutModeCards()
			=> ToggleLayoutMode(FolderLayoutModes.CardsView);

		public void ToggleLayoutModeList()
			=> ToggleLayoutMode(FolderLayoutModes.ListView);

		public void ToggleLayoutModeDetailsView()
			=> ToggleLayoutMode(FolderLayoutModes.DetailsView);

		private void ToggleLayoutMode(FolderLayoutModes layoutMode)
		{
			LayoutMode = layoutMode;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(layoutMode));
		}

		/// <summary>
		/// Applies the specified preferences without saving them.
		/// </summary>
		private void ApplyPreferences(LayoutPreferencesItem preferencesItem)
		{
			_isApplyingPreferences = true;
			try
			{
				LayoutPreferencesItem.TemplateId = preferencesItem.TemplateId;
				LayoutPreferencesItem.FollowsTemplate = preferencesItem.FollowsTemplate;
				OnPropertyChanged(nameof(TemplateId));
				OnPropertyChanged(nameof(IsCustomized));

				if (preferencesItem.DirectorySortOption == SortOption.Property)
					SetSortProperty(preferencesItem.DirectorySortPropertyId);
				else
					DirectorySortOption = preferencesItem.DirectorySortOption;
				DirectorySortDirection = preferencesItem.DirectorySortDirection;
				if (preferencesItem.DirectoryGroupOption == GroupOption.Property)
					SetGroupProperty(preferencesItem.DirectoryGroupPropertyId);
				else
					DirectoryGroupOption = preferencesItem.DirectoryGroupOption;
				DirectoryGroupByDateUnit = preferencesItem.DirectoryGroupByDateUnit;
				DirectoryGroupDirection = preferencesItem.DirectoryGroupDirection;
				SortDirectoriesAlongsideFiles = preferencesItem.SortDirectoriesAlongsideFiles;
				SortFilesFirst = preferencesItem.SortFilesFirst;
				ColumnsViewModel = preferencesItem.ColumnsViewModel;

				if (!IsLayoutModeFixed && LayoutMode != preferencesItem.LayoutMode)
					ToggleLayoutMode(preferencesItem.LayoutMode);
			}
			finally
			{
				_isApplyingPreferences = false;
			}
		}

		private void RequestSave(bool keepsTemplate = false)
		{
			if (_isApplyingPreferences)
				return;

			if (!keepsTemplate && LayoutPreferencesItem.FollowsTemplate)
			{
				LayoutPreferencesItem.FollowsTemplate = false;
				OnPropertyChanged(nameof(IsCustomized));
			}

			LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
		}

		// Static methods

		public static LayoutPreferencesDatabaseManager GetDatabaseManagerInstance()
		{
			return _databaseInstance.Value;
		}

<<<<<<< ours
		public static void SetDefaultLayoutPreferences(ColumnsViewModel columns)
		{
<<<<<<< ours
			UserSettingsService.LayoutSettingsService.ShowDateColumn = !columns.DateModifiedColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn = !columns.DateCreatedColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowTypeColumn = !columns.ItemTypeColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowSizeColumn = !columns.SizeColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowFileTagColumn = !columns.TagColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitStatusColumn = !columns.GitStatusColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitLastCommitDateColumn = !columns.GitLastCommitDateColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitLastCommitMessageColumn = !columns.GitLastCommitMessageColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitCommitAuthorColumn = !columns.GitCommitAuthorColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitLastCommitShaColumn = !columns.GitLastCommitShaColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowDateDeletedColumn = !columns.DateDeletedColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowPathColumn = !columns.PathColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowOriginalPathColumn = !columns.OriginalPathColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowSyncStatusColumn = !columns.StatusColumn.UserCollapsed;

			UserSettingsService.LayoutSettingsService.NameColumnWidth = columns.NameColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.DateModifiedColumnWidth = columns.DateModifiedColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.DateCreatedColumnWidth = columns.DateCreatedColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.TypeColumnWidth = columns.ItemTypeColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.SizeColumnWidth = columns.SizeColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.TagColumnWidth = columns.TagColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitStatusColumnWidth = columns.GitStatusColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitLastCommitDateColumnWidth = columns.GitLastCommitDateColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitLastCommitMessageColumnWidth = columns.GitLastCommitMessageColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitCommitAuthorColumnWidth = columns.GitCommitAuthorColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitLastCommitShaColumnWidth = columns.GitLastCommitShaColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.DateDeletedColumnWidth = columns.DateDeletedColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.PathColumnWidth = columns.PathColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.OriginalPathColumnWidth = columns.OriginalPathColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.SyncStatusColumnWidth = columns.StatusColumn.UserLengthPixels;

			UserSettingsService.LayoutSettingsService.ColumnOrder = [.. columns.ColumnOrder];
=======
			DetailsLayoutColumnDefaults.Save(columns);
>>>>>>> theirs
		}

=======
>>>>>>> theirs
		public static void SetLayoutPreferencesForPath(string? path, LayoutPreferencesItem preferencesItem)
		{
			var folderFRN = Win32Helper.GetFolderFRN(path);
			var trimmedFolderPath = path.TrimPath();
			if (trimmedFolderPath is null)
				return;

			if (preferencesItem.FollowsTemplate && GetLayoutPreferencesFromDatabase(trimmedFolderPath, folderFRN) is not { TemplateId.Length: > 0 })
			{
				var templateId = preferencesItem.TemplateId;
				preferencesItem.TemplateId = string.Empty;
				SetLayoutPreferencesToDatabase(trimmedFolderPath, folderFRN, preferencesItem);
				preferencesItem.TemplateId = templateId;
				return;
			}

<<<<<<< ours
				if (preferencesItem.DirectoryGroupOption != GroupOption.OriginalFolder &&
					preferencesItem.DirectoryGroupOption != GroupOption.DateDeleted &&
					preferencesItem.DirectoryGroupOption != GroupOption.FolderPath &&
					preferencesItem.DirectoryGroupOption != GroupOption.SyncStatus)
				{
					UserSettingsService.LayoutSettingsService.DefaultGroupOption = preferencesItem.DirectoryGroupOption;
					UserSettingsService.LayoutSettingsService.DefaultGroupColumnId = preferencesItem.DirectoryGroupPropertyId;
				}

				UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection = preferencesItem.DirectorySortDirection;
				UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection = preferencesItem.DirectoryGroupDirection;
				UserSettingsService.LayoutSettingsService.DefaultGroupByDateUnit = preferencesItem.DirectoryGroupByDateUnit;
				UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = preferencesItem.SortDirectoriesAlongsideFiles;
				UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst = preferencesItem.SortFilesFirst;

<<<<<<< ours
				UserSettingsService.LayoutSettingsService.NameColumnWidth = preferencesItem.ColumnsViewModel.NameColumn.UserLengthPixels;
				UserSettingsService.LayoutSettingsService.ColumnOrder = [.. preferencesItem.ColumnsViewModel.ColumnOrder];

				if (!preferencesItem.ColumnsViewModel.DateModifiedColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowDateColumn = !preferencesItem.ColumnsViewModel.DateModifiedColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.DateModifiedColumnWidth = preferencesItem.ColumnsViewModel.DateModifiedColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.DateCreatedColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn = !preferencesItem.ColumnsViewModel.DateCreatedColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.DateCreatedColumnWidth = preferencesItem.ColumnsViewModel.DateCreatedColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.ItemTypeColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowTypeColumn = !preferencesItem.ColumnsViewModel.ItemTypeColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.TypeColumnWidth = preferencesItem.ColumnsViewModel.ItemTypeColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.SizeColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowSizeColumn = !preferencesItem.ColumnsViewModel.SizeColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.SizeColumnWidth = preferencesItem.ColumnsViewModel.SizeColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.TagColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowFileTagColumn = !preferencesItem.ColumnsViewModel.TagColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.TagColumnWidth = preferencesItem.ColumnsViewModel.TagColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitStatusColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitStatusColumn = !preferencesItem.ColumnsViewModel.GitStatusColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitStatusColumnWidth = preferencesItem.ColumnsViewModel.GitStatusColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitLastCommitDateColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitLastCommitDateColumn = !preferencesItem.ColumnsViewModel.GitLastCommitDateColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitLastCommitDateColumnWidth = preferencesItem.ColumnsViewModel.GitLastCommitDateColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitLastCommitMessageColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitLastCommitMessageColumn = !preferencesItem.ColumnsViewModel.GitLastCommitMessageColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitLastCommitMessageColumnWidth = preferencesItem.ColumnsViewModel.GitLastCommitMessageColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitCommitAuthorColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitCommitAuthorColumn = !preferencesItem.ColumnsViewModel.GitCommitAuthorColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitCommitAuthorColumnWidth = preferencesItem.ColumnsViewModel.GitCommitAuthorColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitLastCommitShaColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitLastCommitShaColumn = !preferencesItem.ColumnsViewModel.GitLastCommitShaColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitLastCommitShaColumnWidth = preferencesItem.ColumnsViewModel.GitLastCommitShaColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.DateDeletedColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowDateDeletedColumn = !preferencesItem.ColumnsViewModel.DateDeletedColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.DateDeletedColumnWidth = preferencesItem.ColumnsViewModel.DateDeletedColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.PathColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowPathColumn = !preferencesItem.ColumnsViewModel.PathColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.PathColumnWidth = preferencesItem.ColumnsViewModel.PathColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.OriginalPathColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowOriginalPathColumn = !preferencesItem.ColumnsViewModel.OriginalPathColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.OriginalPathColumnWidth = preferencesItem.ColumnsViewModel.OriginalPathColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.StatusColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowSyncStatusColumn = !preferencesItem.ColumnsViewModel.StatusColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.SyncStatusColumnWidth = preferencesItem.ColumnsViewModel.StatusColumn.UserLengthPixels;
				}
=======
				DetailsLayoutColumnDefaults.Save(preferencesItem.ColumnsViewModel);
>>>>>>> theirs
			}
=======
			SetLayoutPreferencesToDatabase(trimmedFolderPath, folderFRN, preferencesItem);
>>>>>>> theirs
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesForPath(string? path, Func<LayoutFolderContent?>? getContent)
		{
			// Guard against null
			if (path is null)
				return null;

			LayoutTemplatesService.CompleteFirstRun();

			//Recycle Bin does not support Column View due to navigation conflicts with hierarchical display
			//Fall back to Details View when Column View is configured
			if (path.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
			{
				var trimmedPath = path.TrimPath() ?? string.Empty;

				var recycleBinPreference = SafetyExtensions.IgnoreExceptions(() =>
				{
					var folderFRN = Win32Helper.GetFolderFRN(trimmedPath);

					return GetLayoutPreferencesFromDatabase(trimmedPath, folderFRN)
						?? GetLayoutPreferencesFromAds(trimmedPath, folderFRN);
				}, App.Logger);

				var preferences = ResolveSavedPreferences(recycleBinPreference, path, null);
				if (preferences.LayoutMode == FolderLayoutModes.ColumnView)
					preferences.LayoutMode = FolderLayoutModes.DetailsView;

				return preferences;
			}

			path = path.TrimPath() ?? string.Empty;

			var preferencesItem = SafetyExtensions.IgnoreExceptions(() =>
			{
				if (path.StartsWith("tag:", StringComparison.Ordinal))
					return GetLayoutPreferencesFromDatabase("Home", null);

				var folderFRN = Win32Helper.GetFolderFRN(path);

				return GetLayoutPreferencesFromDatabase(path, folderFRN)
					?? GetLayoutPreferencesFromAds(path, folderFRN);
			}, App.Logger);

			return ResolveSavedPreferences(preferencesItem, path, getContent);
		}

		private static LayoutPreferencesItem ResolveSavedPreferences(LayoutPreferencesItem? savedPreferences, string path, Func<LayoutFolderContent?>? getContent)
		{
			if (savedPreferences is { FollowsTemplate: false })
				return savedPreferences;

			var preferences = savedPreferences is not null && LayoutTemplatesService.GetTemplate(savedPreferences.TemplateId) is { } template
				? LayoutPreferencesItem.FromTemplate(template)
				: GetTemplatePreferences(path, getContent);

			if (savedPreferences is not null)
				preferences.ColumnsViewModel.ApplyWidths(savedPreferences.ColumnsViewModel);

			return preferences;
		}

		private static LayoutPreferencesItem GetTemplatePreferences(string path, Func<LayoutFolderContent?>? getContent)
		{
			return LayoutPreferencesItem.FromTemplate(LayoutTemplatesService.GetTemplateForPath(path, getContent));
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesFromAds(string path, ulong? frn)
		{
			var str = Win32Helper.ReadStringFromFile($"{path}:files_layoutmode");

			var layoutPreferences = SafetyExtensions.IgnoreExceptions(() => string.IsNullOrEmpty(str)
				? null
				: JsonSerializer.Deserialize(str, AppJsonSerializerContext.Default.LayoutPreferencesItem));

			if (layoutPreferences is null)
				return null;

			// Port settings to the database, delete the ADS
			if (SetLayoutPreferencesToDatabase(path, frn, layoutPreferences))
				PInvoke.DeleteFileFromApp($"{path}:files_layoutmode");

			return layoutPreferences;
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesFromDatabase(string path, ulong? frn)
		{
			if (string.IsNullOrEmpty(path))
				return null;

			var databaseManager = GetDatabaseManagerInstance();

			return databaseManager.GetPreferences(path, frn);
		}

		private static bool SetLayoutPreferencesToDatabase(string path, ulong? frn, LayoutPreferencesItem preferencesItem)
		{
			if (string.IsNullOrEmpty(path))
				return false;

			return SafetyExtensions.IgnoreExceptions(() =>
			{
				GetDatabaseManagerInstance().SetPreferences(path, frn, preferencesItem);
			});
		}

		private bool SetProperty<TValue>(Func<LayoutPreferencesItem, TValue> prop, Action<LayoutPreferencesItem> update, string propertyName)
		{
			var oldValue = prop(LayoutPreferencesItem);
			update(LayoutPreferencesItem);

			if (EqualityComparer<TValue>.Default.Equals(prop(LayoutPreferencesItem), oldValue))
			{
				return false;
			}

			OnPropertyChanged(propertyName);
			return true;
		}
	}
}
