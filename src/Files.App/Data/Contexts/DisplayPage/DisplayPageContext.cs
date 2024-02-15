// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using static Files.App.Constants;

namespace Files.App.Data.Contexts
{
	internal class DisplayPageContext : ObservableObject, IDisplayPageContext
	{
		private readonly IPageContext context = Ioc.Default.GetRequiredService<IPageContext>();
		private readonly IFoldersSettingsService settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public bool IsLayoutAdaptiveEnabled => !settings.SyncFolderPreferencesAcrossDirectories;

		private LayoutTypes _LayoutType = LayoutTypes.None;
		public LayoutTypes LayoutType
		{
			get => _LayoutType;
			set
			{
				var viewModel = FolderSettings;
				if (viewModel is null)
					return;

				switch (value)
				{
					case LayoutTypes.Details:
						viewModel.ToggleLayoutModeDetailsView(true);
						break;
					case LayoutTypes.List:
						viewModel.ToggleLayoutModeList(true);
						break;
					case LayoutTypes.Tiles:
						viewModel.ToggleLayoutModeTiles(true);
						break;
					case LayoutTypes.GridSmall:
						viewModel.ToggleLayoutModeGridView(IconHeights.GridView.Small, true);
						break;
					case LayoutTypes.GridMedium:
						viewModel.ToggleLayoutModeGridView(IconHeights.GridView.Medium, true);
						break;
					case LayoutTypes.GridLarge:
						viewModel.ToggleLayoutModeGridView(IconHeights.GridView.Large, true);
						break;
					case LayoutTypes.Columns:
						viewModel.ToggleLayoutModeColumnView(true);
						break;
					case LayoutTypes.Adaptive:
						viewModel.ToggleLayoutModeAdaptive();
						break;
				}
			}
		}

		private SortOption _SortOption = SortOption.Name;
		public SortOption SortOption
		{
			get => _SortOption;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectorySortOption = value;
			}
		}

		private SortDirection _SortDirection = SortDirection.Ascending;
		public SortDirection SortDirection
		{
			get => _SortDirection;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectorySortDirection = value;
			}
		}

		private GroupOption _GroupOption = GroupOption.None;
		public GroupOption GroupOption
		{
			get => _GroupOption;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectoryGroupOption = value;
			}
		}

		private SortDirection _GroupDirection = SortDirection.Ascending;
		public SortDirection GroupDirection
		{
			get => _GroupDirection;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectoryGroupDirection = value;
			}
		}

		private GroupByDateUnit _GroupByDateUnit = GroupByDateUnit.Year;
		public GroupByDateUnit GroupByDateUnit
		{
			get => _GroupByDateUnit;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectoryGroupByDateUnit = value;
			}
		}

		private bool _SortDirectoriesAlongsideFiles = false;
		public bool SortDirectoriesAlongsideFiles
		{
			get => _SortDirectoriesAlongsideFiles;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.SortDirectoriesAlongsideFiles = value;
			}
		}

		private bool _SortFilesFirst = false;
		public bool SortFilesFirst
		{
			get => _SortFilesFirst;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.SortFilesFirst = value;
			}
		}

		private LayoutPreferencesManager? FolderSettings => context.PaneOrColumn?.InstanceViewModel?.FolderSettings;

		public DisplayPageContext()
		{
			context.Changing += Context_Changing;
			context.Changed += Context_Changed;
			settings.PropertyChanged += Settings_PropertyChanged;
		}

		public void DecreaseLayoutSize()
		{
			if (FolderSettings is LayoutPreferencesManager viewModel)
				viewModel.DecreaseLayoutSize();
		}

		public void IncreaseLayoutSize()
		{
			if (FolderSettings is LayoutPreferencesManager viewModel)
				viewModel.IncreaseLayoutSize();
		}

		private void Context_Changing(object? sender, EventArgs e)
		{
			var viewModel = FolderSettings;
			if (viewModel is not null)
				viewModel.PropertyChanged -= FolderSettings_PropertyChanged;
			Update();
		}
		private void Context_Changed(object? sender, EventArgs e)
		{
			var viewModel = FolderSettings;
			if (viewModel is not null)
				viewModel.PropertyChanged += FolderSettings_PropertyChanged;
			Update();
		}

		private void FolderSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			var viewModel = FolderSettings;
			if (viewModel is null)
				return;

			switch (e.PropertyName)
			{
				case nameof(LayoutPreferencesManager.LayoutMode):
				case nameof(LayoutPreferencesManager.IconHeight):
				case nameof(LayoutPreferencesManager.IsAdaptiveLayoutEnabled):
					SetProperty(ref _LayoutType, GetLayoutType(), nameof(LayoutType));
					break;
				case nameof(LayoutPreferencesManager.DirectorySortOption):
					SetProperty(ref _SortOption, viewModel.DirectorySortOption, nameof(SortOption));
					break;
				case nameof(LayoutPreferencesManager.DirectorySortDirection):
					SetProperty(ref _SortDirection, viewModel.DirectorySortDirection, nameof(SortDirection));
					break;
				case nameof(LayoutPreferencesManager.DirectoryGroupOption):
					SetProperty(ref _GroupOption, viewModel.DirectoryGroupOption, nameof(GroupOption));
					break;
				case nameof(LayoutPreferencesManager.DirectoryGroupDirection):
					SetProperty(ref _GroupDirection, viewModel.DirectoryGroupDirection, nameof(GroupDirection));
					break;
				case nameof(LayoutPreferencesManager.DirectoryGroupByDateUnit):
					SetProperty(ref _GroupByDateUnit, viewModel.DirectoryGroupByDateUnit, nameof(GroupByDateUnit));
					break;
				case nameof(LayoutPreferencesManager.SortDirectoriesAlongsideFiles):
					SetProperty(ref _SortDirectoriesAlongsideFiles, viewModel.SortDirectoriesAlongsideFiles, nameof(SortDirectoriesAlongsideFiles));
					break;
				case nameof(LayoutPreferencesManager.SortFilesFirst):
					SetProperty(ref _SortFilesFirst, viewModel.SortFilesFirst, nameof(SortFilesFirst));
					break;
			}
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.SyncFolderPreferencesAcrossDirectories))
			{
				OnPropertyChanged(nameof(IsLayoutAdaptiveEnabled));
				SetProperty(ref _LayoutType, GetLayoutType(), nameof(LayoutType));
			}
		}

		private void Update()
		{
			var viewModel = FolderSettings;
			if (viewModel is null)
			{
				SetProperty(ref _LayoutType, LayoutTypes.None, nameof(LayoutType));
				SetProperty(ref _SortOption, SortOption.Name, nameof(SortOption));
				SetProperty(ref _SortDirection, SortDirection.Ascending, nameof(SortDirection));
				SetProperty(ref _GroupOption, GroupOption.None, nameof(GroupOption));
				SetProperty(ref _GroupDirection, SortDirection.Ascending, nameof(GroupDirection));
				SetProperty(ref _GroupByDateUnit, GroupByDateUnit.Year, nameof(GroupByDateUnit));
			}
			else
			{
				SetProperty(ref _LayoutType, GetLayoutType(), nameof(LayoutType));
				SetProperty(ref _SortOption, viewModel.DirectorySortOption, nameof(SortOption));
				SetProperty(ref _SortDirection, viewModel.DirectorySortDirection, nameof(SortDirection));
				SetProperty(ref _GroupOption, viewModel.DirectoryGroupOption, nameof(GroupOption));
				SetProperty(ref _GroupDirection, viewModel.DirectoryGroupDirection, nameof(GroupDirection));
				SetProperty(ref _GroupByDateUnit, viewModel.DirectoryGroupByDateUnit, nameof(GroupByDateUnit));
				SetProperty(ref _SortDirectoriesAlongsideFiles, viewModel.SortDirectoriesAlongsideFiles, nameof(SortDirectoriesAlongsideFiles));
				SetProperty(ref _SortFilesFirst, viewModel.SortFilesFirst, nameof(SortFilesFirst));
			}
		}

		private LayoutTypes GetLayoutType()
		{
			var viewModel = FolderSettings;
			if (viewModel is null)
				return LayoutTypes.None;

			bool isAdaptive = IsLayoutAdaptiveEnabled && viewModel.IsAdaptiveLayoutEnabled && !viewModel.IsLayoutModeFixed;
			if (isAdaptive)
				return LayoutTypes.Adaptive;

			return viewModel.LayoutMode switch
			{
				FolderLayoutModes.DetailsView => LayoutTypes.Details,
				FolderLayoutModes.ListView => LayoutTypes.List,
				FolderLayoutModes.TilesView => LayoutTypes.Tiles,
				FolderLayoutModes.GridView => viewModel.IconHeight switch
				{
					< IconHeights.GridView.Medium => LayoutTypes.GridSmall,
					< IconHeights.GridView.Large => LayoutTypes.GridMedium,
					_ => LayoutTypes.GridLarge,
				},
				FolderLayoutModes.ColumnView => LayoutTypes.Columns,
				_ => throw new InvalidEnumArgumentException(),
			};
		}
	}
}
