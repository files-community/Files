// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using static Files.App.Constants.Browser.GridViewBrowser;

namespace Files.App.Contexts
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
					case LayoutTypes.Tiles:
						viewModel.ToggleLayoutModeTiles(true);
						break;
					case LayoutTypes.GridSmall:
						viewModel.ToggleLayoutModeGridViewSmall(true);
						break;
					case LayoutTypes.GridMedium:
						viewModel.ToggleLayoutModeGridViewMedium(true);
						break;
					case LayoutTypes.GridLarge:
						viewModel.ToggleLayoutModeGridViewLarge(true);
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
				if (FolderSettings is FolderSettingsService viewModel)
					viewModel.DirectorySortOption = value;
			}
		}

		private SortDirection _SortDirection = SortDirection.Ascending;
		public SortDirection SortDirection
		{
			get => _SortDirection;
			set
			{
				if (FolderSettings is FolderSettingsService viewModel)
					viewModel.DirectorySortDirection = value;
			}
		}

		private GroupOption _GroupOption = GroupOption.None;
		public GroupOption GroupOption
		{
			get => _GroupOption;
			set
			{
				if (FolderSettings is FolderSettingsService viewModel)
					viewModel.DirectoryGroupOption = value;
			}
		}

		private SortDirection _GroupDirection = SortDirection.Ascending;
		public SortDirection GroupDirection
		{
			get => _GroupDirection;
			set
			{
				if (FolderSettings is FolderSettingsService viewModel)
					viewModel.DirectoryGroupDirection = value;
			}
		}

		private GroupByDateUnit _GroupByDateUnit = GroupByDateUnit.Year;
		public GroupByDateUnit GroupByDateUnit
		{
			get => _GroupByDateUnit;
			set
			{
				if (FolderSettings is FolderSettingsService viewModel)
					viewModel.DirectoryGroupByDateUnit = value;
			}
		}

		private bool _SortDirectoriesAlongsideFiles = false;
		public bool SortDirectoriesAlongsideFiles
		{
			get => _SortDirectoriesAlongsideFiles;
			set
			{
				if (FolderSettings is FolderSettingsService viewModel)
					viewModel.SortDirectoriesAlongsideFiles = value;
			}
		}

		private FolderSettingsService? FolderSettings => context.PaneOrColumn?.InstanceViewModel?.FolderSettings;

		public DisplayPageContext()
		{
			context.Changing += Context_Changing;
			context.Changed += Context_Changed;
			settings.PropertyChanged += Settings_PropertyChanged;
		}

		public void DecreaseLayoutSize()
		{
			if (FolderSettings is FolderSettingsService viewModel)
				viewModel.GridViewSize -= GridViewIncrement;
		}
		public void IncreaseLayoutSize()
		{
			if (FolderSettings is FolderSettingsService viewModel)
				viewModel.GridViewSize += GridViewIncrement;
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
				case nameof(FolderSettingsService.LayoutMode):
				case nameof(FolderSettingsService.GridViewSize):
					SetProperty(ref _LayoutType, GetLayoutType(), nameof(LayoutType));
					break;
				case nameof(FolderSettingsService.DirectorySortOption):
					SetProperty(ref _SortOption, viewModel.DirectorySortOption, nameof(SortOption));
					break;
				case nameof(FolderSettingsService.DirectorySortDirection):
					SetProperty(ref _SortDirection, viewModel.DirectorySortDirection, nameof(SortDirection));
					break;
				case nameof(FolderSettingsService.DirectoryGroupOption):
					SetProperty(ref _GroupOption, viewModel.DirectoryGroupOption, nameof(GroupOption));
					break;
				case nameof(FolderSettingsService.DirectoryGroupDirection):
					SetProperty(ref _GroupDirection, viewModel.DirectoryGroupDirection, nameof(GroupDirection));
					break;
				case nameof(FolderSettingsService.DirectoryGroupByDateUnit):
					SetProperty(ref _GroupByDateUnit, viewModel.DirectoryGroupByDateUnit, nameof(GroupByDateUnit));
					break;
				case nameof(FolderSettingsService.SortDirectoriesAlongsideFiles):
					SetProperty(ref _SortDirectoriesAlongsideFiles, viewModel.SortDirectoriesAlongsideFiles, nameof(SortDirectoriesAlongsideFiles));
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
				FolderLayoutModes.TilesView => LayoutTypes.Tiles,
				FolderLayoutModes.GridView => viewModel.GridViewSizeKind switch
				{
					GridViewSizeKind.Small => LayoutTypes.GridSmall,
					GridViewSizeKind.Medium => LayoutTypes.GridMedium,
					GridViewSizeKind.Large => LayoutTypes.GridLarge,
					_ => throw new InvalidEnumArgumentException(),
				},
				FolderLayoutModes.ColumnView => LayoutTypes.Columns,
				_ => throw new InvalidEnumArgumentException(),
			};
		}
	}
}
