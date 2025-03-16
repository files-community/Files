// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class SortByNameAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.Name;

		public override string Label
			=> Strings.Name.GetLocalizedResource();

		public override string Description
			=> Strings.SortByNameDescription.GetLocalizedResource();
	}

	internal sealed partial class SortByDateModifiedAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.DateModified;

		public override string Label
			=> Strings.DateModifiedLowerCase.GetLocalizedResource();

		public override string Description
			=> Strings.SortByDateModifiedDescription.GetLocalizedResource();
	}

	internal sealed partial class SortByDateCreatedAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.DateCreated;

		public override string Label
			=> Strings.DateCreated.GetLocalizedResource();

		public override string Description
			=> Strings.SortByDateCreatedDescription.GetLocalizedResource();
	}

	internal sealed partial class SortBySizeAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.Size;

		public override string Label
			=> Strings.Size.GetLocalizedResource();

		public override string Description
			=> Strings.SortBySizeDescription.GetLocalizedResource();
	}

	internal sealed partial class SortByTypeAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.FileType;

		public override string Label
			=> Strings.Type.GetLocalizedResource();

		public override string Description
			=> Strings.SortByTypeDescription.GetLocalizedResource();
	}

	internal sealed partial class SortBySyncStatusAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.SyncStatus;

		public override string Label
			=> Strings.SyncStatus.GetLocalizedResource();

		public override string Description
			=> Strings.SortBySyncStatusDescription.GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.CloudDrive;
	}

	internal sealed partial class SortByTagAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.FileTag;

		public override string Label
			=> Strings.FileTags.GetLocalizedResource();

		public override string Description
			=> Strings.SortByTagDescription.GetLocalizedResource();
	}

	internal sealed partial class SortByPathAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.Path;

		public override string Label
			=> Strings.Path.GetLocalizedResource();

		public override string Description
			=> Strings.SortByPathDescription.GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.SearchResults;
	}

	internal sealed partial class SortByOriginalFolderAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.OriginalFolder;

		public override string Label
			=> Strings.OriginalFolder.GetLocalizedResource();

		public override string Description
			=> Strings.SortByOriginalFolderDescription.GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.RecycleBin;
	}

	internal sealed partial class SortByDateDeletedAction : SortByAction
	{
		protected override SortOption SortOption
			=> SortOption.DateDeleted;

		public override string Label
			=> Strings.DateDeleted.GetLocalizedResource();

		public override string Description
			=> Strings.SortByDateDeletedDescription.GetLocalizedResource();

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
			=> Strings.Ascending.GetLocalizedResource();

		public string Description
			=> Strings.SortAscendingDescription.GetLocalizedResource();

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
			=> Strings.Descending.GetLocalizedResource();

		public string Description
			=> Strings.SortDescendingDescription.GetLocalizedResource();

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
			=> Strings.ToggleSortDirection.GetLocalizedResource();

		public string Description
			=> Strings.ToggleSortDirectionDescription.GetLocalizedResource();

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
