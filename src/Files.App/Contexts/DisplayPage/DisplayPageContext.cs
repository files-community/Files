using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.ViewModels;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using System;
using System.ComponentModel;
using static Files.App.Constants.Browser.GridViewBrowser;

namespace Files.App.Contexts
{
	internal class DisplayPageContext : ObservableObject, IDisplayPageContext
	{
		private readonly IPageContext context = Ioc.Default.GetRequiredService<IPageContext>();
		private readonly IFoldersSettingsService settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		public bool IsLayoutAdaptiveEnabled => !settings.SyncFolderPreferencesAcrossDirectories;

		private LayoutTypes layoutType = LayoutTypes.None;
		public LayoutTypes LayoutType
		{
			get => layoutType;
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

		private SortOption sortOption = SortOption.Name;
		public SortOption SortOption
		{
			get => sortOption;
			set
			{
				if (FolderSettings is FolderSettingsViewModel viewModel)
					viewModel.DirectorySortOption = value;
			}
		}

		private SortDirection sortDirection = SortDirection.Ascending;
		public SortDirection SortDirection
		{
			get => sortDirection;
			set
			{
				if (FolderSettings is FolderSettingsViewModel viewModel)
					viewModel.DirectorySortDirection = value;
			}
		}

		private GroupOption groupOption = GroupOption.None;
		public GroupOption GroupOption
		{
			get => groupOption;
			set
			{
				if (FolderSettings is FolderSettingsViewModel viewModel)
					viewModel.DirectoryGroupOption = value;
			}
		}

		private SortDirection groupDirection = SortDirection.Ascending;
		public SortDirection GroupDirection
		{
			get => groupDirection;
			set
			{
				if (FolderSettings is FolderSettingsViewModel viewModel)
					viewModel.DirectoryGroupDirection = value;
			}
		}

		private bool sortDirectoriesAlongsideFiles = false;
		public bool SortDirectoriesAlongsideFiles
		{
			get => sortDirectoriesAlongsideFiles;
			set
			{
				if (FolderSettings is FolderSettingsViewModel viewModel)
					viewModel.SortDirectoriesAlongsideFiles = value;
			}
		}

		private FolderSettingsViewModel? FolderSettings => context.PaneOrColumn?.InstanceViewModel?.FolderSettings;

		public DisplayPageContext()
		{
			context.Changing += Context_Changing;
			context.Changed += Context_Changed;
			settings.PropertyChanged += Settings_PropertyChanged;
		}

		public void DecreaseLayoutSize()
		{
			if (FolderSettings is FolderSettingsViewModel viewModel)
				viewModel.GridViewSize -= GridViewIncrement;
		}
		public void IncreaseLayoutSize()
		{
			if (FolderSettings is FolderSettingsViewModel viewModel)
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
				case nameof(FolderSettingsViewModel.LayoutMode):
				case nameof(FolderSettingsViewModel.GridViewSize):
					SetProperty(ref layoutType, GetLayoutType(), nameof(LayoutType));
					break;
				case nameof(FolderSettingsViewModel.DirectorySortOption):
					SetProperty(ref sortOption, viewModel.DirectorySortOption, nameof(SortOption));
					break;
				case nameof(FolderSettingsViewModel.DirectorySortDirection):
					SetProperty(ref sortDirection, viewModel.DirectorySortDirection, nameof(SortDirection));
					break;
				case nameof(FolderSettingsViewModel.DirectoryGroupOption):
					SetProperty(ref groupOption, viewModel.DirectoryGroupOption, nameof(GroupOption));
					break;
				case nameof(FolderSettingsViewModel.DirectoryGroupDirection):
					SetProperty(ref groupDirection, viewModel.DirectoryGroupDirection, nameof(GroupDirection));
					break;
				case nameof(FolderSettingsViewModel.SortDirectoriesAlongsideFiles):
					SetProperty(ref sortDirectoriesAlongsideFiles, viewModel.SortDirectoriesAlongsideFiles, nameof(SortDirectoriesAlongsideFiles));
					break;
			}
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.SyncFolderPreferencesAcrossDirectories))
			{
				OnPropertyChanged(nameof(IsLayoutAdaptiveEnabled));
				SetProperty(ref layoutType, GetLayoutType(), nameof(LayoutType));
			}
		}

		private void Update()
		{
			var viewModel = FolderSettings;
			if (viewModel is null)
			{
				SetProperty(ref layoutType, LayoutTypes.None, nameof(LayoutType));
				SetProperty(ref sortOption, SortOption.Name, nameof(SortOption));
				SetProperty(ref sortDirection, SortDirection.Ascending, nameof(SortDirection));
				SetProperty(ref groupOption, GroupOption.None, nameof(GroupOption));
				SetProperty(ref groupDirection, SortDirection.Ascending, nameof(GroupDirection));
			}
			else
			{
				SetProperty(ref layoutType, GetLayoutType(), nameof(LayoutType));
				SetProperty(ref sortOption, viewModel.DirectorySortOption, nameof(SortOption));
				SetProperty(ref sortDirection, viewModel.DirectorySortDirection, nameof(SortDirection));
				SetProperty(ref groupOption, viewModel.DirectoryGroupOption, nameof(GroupOption));
				SetProperty(ref groupDirection, viewModel.DirectoryGroupDirection, nameof(GroupDirection));
				SetProperty(ref sortDirectoriesAlongsideFiles, viewModel.SortDirectoriesAlongsideFiles, nameof(SortDirectoriesAlongsideFiles));
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
