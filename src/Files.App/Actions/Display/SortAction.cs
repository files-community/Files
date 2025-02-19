// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class SortByNameAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.Name;

		public override string Label
			=> "Name".GetLocalizedResource();

		public override string Description
			=> "SortByNameDescription".GetLocalizedResource();
	}

	internal sealed partial class SortByDateModifiedAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.DateModified;

		public override string Label
			=> "DateModifiedLowerCase".GetLocalizedResource();

		public override string Description
			=> "SortByDateModifiedDescription".GetLocalizedResource();
	}

	internal sealed partial class SortByDateCreatedAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.DateCreated;

		public override string Label
			=> "DateCreated".GetLocalizedResource();

		public override string Description
			=> "SortByDateCreatedDescription".GetLocalizedResource();
	}

	internal sealed partial class SortBySizeAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.Size;

		public override string Label
			=> "Size".GetLocalizedResource();

		public override string Description
			=> "SortBySizeDescription".GetLocalizedResource();
	}

	internal sealed partial class SortByTypeAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.FileType;

		public override string Label
			=> "Type".GetLocalizedResource();

		public override string Description
			=> "SortByTypeDescription".GetLocalizedResource();
	}

	internal sealed partial class SortBySyncStatusAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.SyncStatus;

		public override string Label
			=> "SyncStatus".GetLocalizedResource();

		public override string Description
			=> "SortBySyncStatusDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.CloudDrive;
	}

	internal sealed partial class SortByTagAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.FileTag;

		public override string Label
			=> "FileTags".GetLocalizedResource();

		public override string Description
			=> "SortByTagDescription".GetLocalizedResource();
	}

	internal sealed partial class SortByPathAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.Path;

		public override string Label
			=> "Path".GetLocalizedResource();

		public override string Description
			=> "SortByPathDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.SearchResults;
	}

	internal sealed partial class SortByOriginalFolderAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.OriginalFolder;

		public override string Label
			=> "OriginalFolder".GetLocalizedResource();

		public override string Description
			=> "SortByOriginalFolderDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.RecycleBin;
	}

	internal sealed partial class SortByDateDeletedAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.DateDeleted;

		public override string Label
			=> "DateDeleted".GetLocalizedResource();

		public override string Description
			=> "SortByDateDeletedDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.RecycleBin;
	}

	internal abstract class SortByAction : ObservableObject, IToggleAction
	{
		private readonly IContentPageContext contentContext;

		private readonly IDisplayPageContext displayContext;

		protected abstract SortOption SortOption { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		public bool IsOn
			=> displayContext.SortOption == SortOption;

		public bool IsExecutable
			=> GetIsExecutable(contentContext.PageType);

		public SortByAction()
		{
			contentContext = Ioc.Default.GetRequiredService<IContentPageContext>();
			displayContext = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			contentContext.PropertyChanged += ContentContext_PropertyChanged;
			displayContext.PropertyChanged += DisplayContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			displayContext.SortOption = SortOption;
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		protected virtual bool GetIsExecutable(ContentPageTypes pageType) => true;

		private void ContentContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortOption))
				OnPropertyChanged(nameof(IsOn));
		}
	}

	internal sealed partial class SortAscendingAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "Ascending".GetLocalizedResource();

		public string Description
			=> "SortAscendingDescription".GetLocalizedResource();

		public bool IsOn
			=> context.SortDirection is SortDirection.Ascending;

		public SortAscendingAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.SortDirection = SortDirection.Ascending;
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortDirection))
				OnPropertyChanged(nameof(IsOn));
		}
	}

	internal sealed partial class SortDescendingAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "Descending".GetLocalizedResource();

		public string Description
			=> "SortDescendingDescription".GetLocalizedResource();

		public bool IsOn
			=> context.SortDirection is SortDirection.Descending;

		public SortDescendingAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.SortDirection = SortDirection.Descending;
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortDirection))
				OnPropertyChanged(nameof(IsOn));
		}
	}

	internal sealed class ToggleSortDirectionAction : IAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "ToggleSortDirection".GetLocalizedResource();

		public string Description
			=> "ToggleSortDirectionDescription".GetLocalizedResource();

		public ToggleSortDirectionAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.SortDirection =
				context.SortDirection is SortDirection.Descending
					? SortDirection.Ascending
					: SortDirection.Descending;

			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}
	}
}
