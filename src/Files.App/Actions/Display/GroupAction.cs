// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class GroupByNoneAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.None;

		public override string Label { get; } = "None".GetLocalizedResource();

		public override string Description => "GroupByNoneDescription".GetLocalizedResource();
	}

	internal class GroupByNameAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.Name;

		public override string Label { get; } = "Name".GetLocalizedResource();

		public override string Description => "GroupByNameDescription".GetLocalizedResource();
	}

	internal class GroupByDateModifiedAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.DateModified;

		public override string Label { get; } = "DateModifiedLowerCase".GetLocalizedResource();

		public override string Description => "GroupByDateModifiedDescription".GetLocalizedResource();
	}

	internal class GroupByDateCreatedAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.DateCreated;

		public override string Label { get; } = "DateCreated".GetLocalizedResource();

		public override string Description => "GroupByDateCreatedDescription".GetLocalizedResource();
	}

	internal class GroupBySizeAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.Size;

		public override string Label { get; } = "Size".GetLocalizedResource();

		public override string Description => "GroupBySizeDescription".GetLocalizedResource();
	}

	internal class GroupByTypeAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.FileType;

		public override string Label { get; } = "Type".GetLocalizedResource();

		public override string Description => "GroupByTypeDescription".GetLocalizedResource();
	}

	internal class GroupBySyncStatusAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.SyncStatus;

		public override string Label { get; } = "SyncStatus".GetLocalizedResource();

		public override string Description => "GroupBySyncStatusDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.CloudDrive;
	}

	internal class GroupByTagAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.FileTag;

		public override string Label { get; } = "FileTags".GetLocalizedResource();

		public override string Description => "GroupByTagDescription".GetLocalizedResource();
	}

	internal class GroupByOriginalFolderAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.OriginalFolder;

		public override string Label { get; } = "OriginalFolder".GetLocalizedResource();

		public override string Description => "GroupByOriginalFolderDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.CloudDrive;
	}

	internal class GroupByDateDeletedAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.DateDeleted;

		public override string Label { get; } = "DateDeleted".GetLocalizedResource();

		public override string Description => "GroupByDateDeletedDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.RecycleBin;
	}

	internal class GroupByFolderPathAction : GroupByAction
	{
		protected override GroupOption GroupOption { get; } = GroupOption.FolderPath;

		public override string Label { get; } = "FolderPath".GetLocalizedResource();

		public override string Description => "GroupByFolderPathDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType) => pageType is ContentPageTypes.Library;
	}

	internal abstract class GroupByAction : ObservableObject, IToggleAction
	{
		protected IContentPageContext ContentContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected IDisplayPageContext DisplayContext { get; } = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		protected abstract GroupOption GroupOption { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		private bool isOn;
		public bool IsOn => isOn;

		private bool isExecutable = false;
		public bool IsExecutable => isExecutable;

		public GroupByAction()
		{
			isOn = DisplayContext.GroupOption == GroupOption;
			isExecutable = GetIsExecutable(ContentContext.PageType);

			ContentContext.PropertyChanged += ContentContext_PropertyChanged;
			DisplayContext.PropertyChanged += DisplayContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			DisplayContext.GroupOption = GroupOption;
			return Task.CompletedTask;
		}

		protected virtual bool GetIsExecutable(ContentPageTypes pageType) => true;

		private void ContentContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				SetProperty(ref isExecutable, GetIsExecutable(ContentContext.PageType), nameof(IsExecutable));
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.GroupOption))
				SetProperty(ref isOn, DisplayContext.GroupOption == GroupOption, nameof(IsOn));
		}
	}

	internal class GroupAscendingAction : ObservableObject, IToggleAction
	{
		private	readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "Ascending".GetLocalizedResource();

		public string Description => "GroupAscendingDescription".GetLocalizedResource();

		public bool IsOn => context.GroupDirection is SortDirection.Ascending;
		public bool IsExecutable => context.GroupOption is not GroupOption.None;

		public GroupAscendingAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.GroupDirection = SortDirection.Ascending;
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IDisplayPageContext.GroupOption):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IDisplayPageContext.GroupDirection):
					OnPropertyChanged(nameof(IsOn));
					break;
			}
		}
	}

	internal class GroupDescendingAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "Descending".GetLocalizedResource();

		public string Description => "GroupDescendingDescription".GetLocalizedResource();

		public bool IsOn => context.GroupDirection is SortDirection.Descending;
		public bool IsExecutable => context.GroupOption is not GroupOption.None;

		public GroupDescendingAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			context.GroupDirection = SortDirection.Descending;
			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IDisplayPageContext.GroupOption):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IDisplayPageContext.GroupDirection):
					OnPropertyChanged(nameof(IsOn));
					break;
			}
		}
	}

	internal class ToggleGroupDirectionAction : IAction
	{
		private readonly IDisplayPageContext context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

		public string Label { get; } = "ToggleSortDirection".GetLocalizedResource();

		public string Description => "ToggleGroupDirectionDescription".GetLocalizedResource();

		public Task ExecuteAsync()
		{
			context.GroupDirection = context.SortDirection is SortDirection.Descending ? SortDirection.Ascending : SortDirection.Descending;
			return Task.CompletedTask;
		}
	}
}
