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

		public override string Description => "SortByNameDescription".GetLocalizedResource();
	}

	internal class SortByDateModifiedAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.DateModified;

		public override string Label { get; } = "DateModifiedLowerCase".GetLocalizedResource();

		public override string Description => "SortByDateModifiedDescription".GetLocalizedResource();
	}

	internal class SortByDateCreatedAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.DateCreated;

		public override string Label { get; } = "DateCreated".GetLocalizedResource();

		public override string Description => "SortByDateCreatedDescription".GetLocalizedResource();
	}

	internal class SortBySizeAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.Size;

		public override string Label { get; } = "Size".GetLocalizedResource();

		public override string Description => "SortBySizeDescription".GetLocalizedResource();
	}

	internal class SortByTypeAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.FileType;

		public override string Label { get; } = "Type".GetLocalizedResource();

		public override string Description => "SortByTypeDescription".GetLocalizedResource();
	}

	internal class SortBySyncStatusAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.SyncStatus;

		public override string Label { get; } = "SyncStatus".GetLocalizedResource();

		public override string Description => "SortBySyncStatusDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.CloudDrive;
	}

	internal class SortByTagAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.FileTag;

		public override string Label { get; } = "FileTags".GetLocalizedResource();

		public override string Description => "SortByTagDescription".GetLocalizedResource();
	}

	internal class SortByOriginalFolderAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.OriginalFolder;

		public override string Label { get; } = "OriginalFolder".GetLocalizedResource();

		public override string Description => "SortByOriginalFolderDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.CloudDrive;
	}

	internal class SortByDateDeletedAction : SortByAction
	{
		protected override SortOption SortOption { get; } = SortOption.DateDeleted;

		public override string Label { get; } = "DateDeleted".GetLocalizedResource();

		public override string Description => "SortByDateDeletedDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.RecycleBin;
	}

	internal abstract class SortByAction : ObservableObject, IToggleAction
	{
		private readonly IContentPageContext contentContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IDisplayPageContext displayContext = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		protected abstract SortOption SortOption { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		private bool isOn;
		public bool IsOn => isOn;

		private bool isExecutable = false;
		public bool IsExecutable => isExecutable;

		public SortByAction()
		{
			isOn = displayContext.SortOption == SortOption;
			isExecutable = GetIsExecutable(contentContext.PageType);

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
				SetProperty(ref isExecutable, GetIsExecutable(contentContext.PageType), nameof(IsExecutable));
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.SortOption))
				SetProperty(ref isOn, displayContext.SortOption == SortOption, nameof(IsOn));
		}
	}

	internal class SortAscendingAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "Ascending".GetLocalizedResource();

		public string Description => "SortAscendingDescription".GetLocalizedResource();

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
				OnPropertyChanged(nameof(IsOn));
		}
	}

	internal class SortDescendingAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "Descending".GetLocalizedResource();

		public string Description => "SortDescendingDescription".GetLocalizedResource();

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
				OnPropertyChanged(nameof(IsOn));
		}
	}

	internal class ToggleSortDirectionAction : IAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "ToggleSortDirection".GetLocalizedResource();

		public string Description => "ToggleSortDirectionDescription".GetLocalizedResource();

		public Task ExecuteAsync()
		{
			context.SortDirection = context.SortDirection is SortDirection.Descending ? SortDirection.Ascending : SortDirection.Descending;
			return Task.CompletedTask;
		}
	}
}
