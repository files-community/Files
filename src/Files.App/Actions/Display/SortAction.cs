using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.Shared.Enums;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class SortByNameAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.Name;

		public override string Label { get; } = "Name".GetLocalizedResource();
	}

	internal class SortByDateModifiedAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.DateModified;

		public override string Label { get; } = "DateModifiedLowerCase".GetLocalizedResource();
	}

	internal class SortByDateCreatedAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.DateCreated;

		public override string Label { get; } = "DateCreated".GetLocalizedResource();
	}

	internal class SortBySizeAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.Size;

		public override string Label { get; } = "Size".GetLocalizedResource();
	}

	internal class SortByTypeAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.FileType;

		public override string Label { get; } = "Type".GetLocalizedResource();
	}

	internal class SortBySyncStatusAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.SyncStatus;

		public override string Label { get; } = "SyncStatus".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.CloudDrive;
	}

	internal class SortByTagAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.FileTag;

		public override string Label { get; } = "FileTags".GetLocalizedResource();
	}

	internal class SortByOriginalFolderAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.OriginalFolder;

		public override string Label { get; } = "OriginalFolder".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.CloudDrive;
	}

	internal class SortByDateDeletedAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.DateDeleted;

		public override string Label { get; } = "DateDeleted".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.RecycleBin;
	}

	internal abstract class SortByAction : ToggleAction
	{
		private IContentPageContext contentContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IDisplayPageContext displayContext = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		protected abstract SortOption SortOption { get; }

		public abstract string Label { get; }

		public string Description => "TODO: Need to be described.";

		public bool IsOn => displayContext.SortOption == SortOption;

		public bool CanExecute => GetIsExecutable(contentContext.PageType);

		public SortByAction()
		{
			contentContext.PropertyChanged += ContentContext_PropertyChanged;
			displayContext.PropertyChanged += DisplayContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			displayContext.SortOption = SortOption;
			return Task.CompletedTask;
		}

		protected virtual bool GetIsExecutable(ContentPageTypes pageType) => true;

		private void ContentContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				NotifyCanExecuteChanged();		
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortOption))
				NotifyCanExecuteChanged();
		}
	}

	internal class SortAscendingAction : ToggleAction
	{
		private IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "Ascending".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public bool IsOn => context.SortDirection is SortDirection.Ascending;

		public SortAscendingAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.SortDirection = SortDirection.Ascending;
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortDirection))
				NotifyCanExecuteChanged();
		}
	}

	internal class SortDescendingAction : ToggleAction
	{
		private IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "Descending".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public bool IsOn => context.SortDirection is SortDirection.Descending;

		public SortDescendingAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.SortDirection = SortDirection.Descending;
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortDirection))
				NotifyCanExecuteChanged();
		}
	}

	internal class ToggleSortDirectionAction : XamlUICommand
	{
		private IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "ToggleSortDirection".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public Task ExecuteAsync()
		{
			context.SortDirection = context.SortDirection is SortDirection.Descending ? SortDirection.Ascending : SortDirection.Descending;
			return Task.CompletedTask;
		}
	}
}
