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

		public string DirectorySortColumnId
			=> LayoutPreferencesItem.DirectorySortColumnId;

		public void SetSortColumn(string columnId)
		{
			if (DirectorySortOption == SortOption.Column && DirectorySortColumnId == columnId)
				return;

			LayoutPreferencesItem.DirectorySortColumnId = columnId;
			LayoutPreferencesItem.DirectorySortOption = SortOption.Column;
			OnPropertyChanged(nameof(DirectorySortColumnId));
			OnPropertyChanged(nameof(DirectorySortOption));
			RequestSave();
			SortOptionPreferenceUpdated?.Invoke(this, SortOption.Column);
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

		public string DirectoryGroupColumnId
			=> LayoutPreferencesItem.DirectoryGroupColumnId;

		public void SetGroupColumn(string columnId)
		{
			if (DirectoryGroupOption == GroupOption.Column && DirectoryGroupColumnId == columnId)
				return;

			LayoutPreferencesItem.DirectoryGroupColumnId = columnId;
			LayoutPreferencesItem.DirectoryGroupOption = GroupOption.Column;
			OnPropertyChanged(nameof(DirectoryGroupColumnId));
			OnPropertyChanged(nameof(DirectoryGroupOption));
			RequestSave();
			GroupOptionPreferenceUpdated?.Invoke(this, GroupOption.Column);
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
			}

			return (preferencesItem.LayoutMode) switch
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
			if (IsLayoutModeFixed || !LayoutTemplatesService.HasContentRules || string.IsNullOrEmpty(trimmedPath) || !IsPathUsingDefaultLayout(trimmedPath))
				return;

			var template = LayoutTemplatesService.GetTemplateForPath(trimmedPath, () => FolderContentHelpers.GetContent(items));
			if (template.Id != TemplateId)
				ApplyPreferences(LayoutPreferencesItem.FromTemplate(template));
		}

		public bool IsPathUsingDefaultLayout(string? path)
		{
			return string.IsNullOrEmpty(path) ||
				GetLayoutPreferencesFromDatabase(path, Win32Helper.GetFolderFRN(path)) is null;
		}

		/// <summary>
		/// Saves the preferences of the specified folder to its template, and makes the folder use the template again.
		/// </summary>
		public static void UpdateTemplateFromPath(string path)
		{
			if (GetLayoutPreferences(path) is not { } preferences ||
				LayoutTemplatesService.GetTemplate(preferences.TemplateId)?.Clone() is not { } template)
				return;

			preferences.SaveToTemplate(template);
			LayoutTemplatesService.SaveTemplate(template);
			SetTemplateForPath(path, template.Id);
		}

		/// <summary>
		/// Saves the preferences of the specified folder as a new template, and makes the folder use it.
		/// </summary>
		public static LayoutTemplate? SaveAsNewTemplateFromPath(string path, string name)
		{
			if (GetLayoutPreferences(path) is not { } preferences)
				return null;

			var template = (LayoutTemplatesService.GetTemplate(preferences.TemplateId) ?? LayoutTemplatesService.DefaultTemplate).Clone();
			preferences.SaveToTemplate(template);

			var newTemplate = LayoutTemplatesService.AddTemplate(template, name);
			SetTemplateForPath(path, newTemplate.Id);
			return newTemplate;
		}

		/// <summary>
		/// Gets the preferences the specified folder uses.
		/// </summary>
		public static LayoutPreferencesItem? GetLayoutPreferences(string? path)
			=> GetLayoutPreferencesForPath(path, () => FolderContentHelpers.ReadContent(path));

		/// <summary>
		/// Makes the specified folder use the specified template, discarding its own preferences.
		/// </summary>
		/// <remarks>
		/// The folder keeps the template from then on, even when its file types match another template.
		/// </remarks>
		public static void SetTemplateForPath(string path, string templateId)
		{
			var trimmedPath = path.TrimPath();
			if (string.IsNullOrEmpty(trimmedPath))
				return;

			SafetyExtensions.IgnoreExceptions(() =>
			{
				var folderFRN = Win32Helper.GetFolderFRN(trimmedPath);
				GetDatabaseManagerInstance().SetPreferences(trimmedPath, folderFRN, new LayoutPreferencesItem() { TemplateId = templateId, FollowsTemplate = true });
			}, App.Logger);
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

				if (preferencesItem.DirectorySortOption == SortOption.Column)
					SetSortColumn(preferencesItem.DirectorySortColumnId);
				else
					DirectorySortOption = preferencesItem.DirectorySortOption;
				DirectorySortDirection = preferencesItem.DirectorySortDirection;
				if (preferencesItem.DirectoryGroupOption == GroupOption.Column)
					SetGroupColumn(preferencesItem.DirectoryGroupColumnId);
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

		private void RequestSave()
		{
			if (_isApplyingPreferences)
				return;

			if (LayoutPreferencesItem.FollowsTemplate)
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

		public static void SetLayoutPreferencesForPath(string? path, LayoutPreferencesItem preferencesItem)
		{
			if (preferencesItem.FollowsTemplate)
				return;

			var folderFRN = Win32Helper.GetFolderFRN(path);
			var trimmedFolderPath = path.TrimPath();
			if (trimmedFolderPath is not null)
				SetLayoutPreferencesToDatabase(trimmedFolderPath, folderFRN, preferencesItem);
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesForPath(string? path, Func<LayoutFolderContent?>? getContent)
		{
			// Guard against null
			if (path is null)
				return null;

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

			if (savedPreferences is not null && LayoutTemplatesService.GetTemplate(savedPreferences.TemplateId) is { } template)
				return LayoutPreferencesItem.FromTemplate(template);

			return GetTemplatePreferences(path, getContent);
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
