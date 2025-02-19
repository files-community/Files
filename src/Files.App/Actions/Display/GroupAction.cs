﻿// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class GroupByNoneAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.None;

		public override string Label
			=> "None".GetLocalizedResource();

		public override string Description
			=> "GroupByNoneDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByNameAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.Name;

		public override string Label
			=> "Name".GetLocalizedResource();

		public override string Description
			=> "GroupByNameDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByDateModifiedAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateModified;

		public override string Label
			=> "DateModifiedLowerCase".GetLocalizedResource();

		public override string Description
			=> "GroupByDateModifiedDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByDateCreatedAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateCreated;

		public override string Label
			=> "DateCreated".GetLocalizedResource();

		public override string Description
			=> "GroupByDateCreatedDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupBySizeAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.Size;

		public override string Label
			=> "Size".GetLocalizedResource();

		public override string Description
			=> "GroupBySizeDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByTypeAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.FileType;

		public override string Label
			=> "Type".GetLocalizedResource();

		public override string Description
			=> "GroupByTypeDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupBySyncStatusAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.SyncStatus;

		public override string Label
			=> "SyncStatus".GetLocalizedResource();

		public override string Description
			=> "GroupBySyncStatusDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.CloudDrive;
	}

	internal sealed partial class GroupByTagAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.FileTag;

		public override string Label
			=> "FileTags".GetLocalizedResource();

		public override string Description
			=> "GroupByTagDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByOriginalFolderAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.OriginalFolder;

		public override string Label
			=> "OriginalFolder".GetLocalizedResource();

		public override string Description
			=> "GroupByOriginalFolderDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.RecycleBin;
	}

	internal sealed partial class GroupByDateDeletedAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateDeleted;

		public override string Label
			=> "DateDeleted".GetLocalizedResource();

		public override string Description
			=> "GroupByDateDeletedDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.RecycleBin;
	}

	internal sealed partial class GroupByFolderPathAction : GroupByAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.FolderPath;

		public override string Label
			=> "FolderPath".GetLocalizedResource();

		public override string Description
			=> "GroupByFolderPathDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.Library or ContentPageTypes.SearchResults;
	}

	internal abstract class GroupByAction : ObservableObject, IToggleAction
	{
		protected IContentPageContext ContentContext;

		protected IDisplayPageContext DisplayContext;

		protected abstract GroupOption GroupOption { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		public bool IsOn
			=> DisplayContext.GroupOption == GroupOption;

		public bool IsExecutable
			=> GetIsExecutable(ContentContext.PageType);

		public GroupByAction()
		{
			ContentContext = Ioc.Default.GetRequiredService<IContentPageContext>();
			DisplayContext = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			ContentContext.PropertyChanged += ContentContext_PropertyChanged;
			DisplayContext.PropertyChanged += DisplayContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			DisplayContext.GroupOption = GroupOption;
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		protected virtual bool GetIsExecutable(ContentPageTypes pageType)
		{
			return true;
		}

		private void ContentContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.GroupOption))
				OnPropertyChanged(nameof(IsOn));
		}
	}

	internal sealed partial class GroupByDateModifiedYearAction : GroupByDateAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateModified;

		protected override GroupByDateUnit GroupByDateUnit
			=> GroupByDateUnit.Year;

		public override string Label
			=> "Year".GetLocalizedResource();

		public override string Description
			=> "GroupByDateModifiedYearDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByDateModifiedMonthAction : GroupByDateAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateModified;

		protected override GroupByDateUnit GroupByDateUnit
			=> GroupByDateUnit.Month;

		public override string Label
			=> "Month".GetLocalizedResource();

		public override string Description
			=> "GroupByDateModifiedMonthDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByDateModifiedDayAction : GroupByDateAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateModified;

		protected override GroupByDateUnit GroupByDateUnit
			=> GroupByDateUnit.Day;

		public override string Label
			=> "Day".GetLocalizedResource();

		public override string Description
			=> "GroupByDateModifiedDayDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByDateCreatedYearAction : GroupByDateAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateCreated;

		protected override GroupByDateUnit GroupByDateUnit
			=> GroupByDateUnit.Year;

		public override string Label
			=> "Year".GetLocalizedResource();

		public override string Description
			=> "GroupByDateCreatedYearDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByDateCreatedMonthAction : GroupByDateAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateCreated;

		protected override GroupByDateUnit GroupByDateUnit
			=> GroupByDateUnit.Month;

		public override string Label
			=> "Month".GetLocalizedResource();

		public override string Description
			=> "GroupByDateCreatedMonthDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByDateCreatedDayAction : GroupByDateAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateCreated;

		protected override GroupByDateUnit GroupByDateUnit
			=> GroupByDateUnit.Day;

		public override string Label
			=> "Day".GetLocalizedResource();

		public override string Description
			=> "GroupByDateCreatedDayDescription".GetLocalizedResource();
	}

	internal sealed partial class GroupByDateDeletedYearAction : GroupByDateAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateDeleted;

		protected override GroupByDateUnit GroupByDateUnit
			=> GroupByDateUnit.Year;

		public override string Label
			=> "Year".GetLocalizedResource();

		public override string Description
			=> "GroupByDateDeletedYearDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.RecycleBin;
	}

	internal sealed partial class GroupByDateDeletedMonthAction : GroupByDateAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateDeleted;

		protected override GroupByDateUnit GroupByDateUnit
			=> GroupByDateUnit.Month;

		public override string Label
			=> "Month".GetLocalizedResource();

		public override string Description
			=> "GroupByDateDeletedMonthDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.RecycleBin;
	}

	internal sealed partial class GroupByDateDeletedDayAction : GroupByDateAction
	{
		protected override GroupOption GroupOption
			=> GroupOption.DateDeleted;

		protected override GroupByDateUnit GroupByDateUnit
			=> GroupByDateUnit.Day;

		public override string Label
			=> "Day".GetLocalizedResource();

		public override string Description
			=> "GroupByDateDeletedDayDescription".GetLocalizedResource();

		protected override bool GetIsExecutable(ContentPageTypes pageType)
			=> pageType is ContentPageTypes.RecycleBin;
	}

	internal abstract class GroupByDateAction : ObservableObject, IToggleAction
	{
		protected IContentPageContext ContentContext;

		protected IDisplayPageContext DisplayContext;

		protected abstract GroupOption GroupOption { get; }

		protected abstract GroupByDateUnit GroupByDateUnit { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		public bool IsOn =>
			DisplayContext.GroupOption == GroupOption &&
			DisplayContext.GroupByDateUnit == GroupByDateUnit;

		public bool IsExecutable
			=> GetIsExecutable(ContentContext.PageType);

		public GroupByDateAction()
		{
			ContentContext = Ioc.Default.GetRequiredService<IContentPageContext>();
			DisplayContext = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			ContentContext.PropertyChanged += ContentContext_PropertyChanged;
			DisplayContext.PropertyChanged += DisplayContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			DisplayContext.GroupOption = GroupOption;
			DisplayContext.GroupByDateUnit = GroupByDateUnit;
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		protected virtual bool GetIsExecutable(ContentPageTypes pageType)
		{
			return true;
		}

		private void ContentContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.PageType))
				OnPropertyChanged(nameof(IsExecutable));
		}

		private void DisplayContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.GroupOption) or nameof(IDisplayPageContext.GroupByDateUnit))
				OnPropertyChanged(nameof(IsOn));
		}
	}

	internal sealed partial class GroupAscendingAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "Ascending".GetLocalizedResource();

		public string Description
			=> "GroupAscendingDescription".GetLocalizedResource();

		public bool IsOn
			=> context.GroupDirection is SortDirection.Ascending;

		public bool IsExecutable
			=> context.GroupOption is not GroupOption.None;

		public GroupAscendingAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.GroupDirection = SortDirection.Ascending;
			LayoutHelpers.UpdateOpenTabsPreferences();

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

	internal sealed partial class GroupDescendingAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "Descending".GetLocalizedResource();

		public string Description
			=> "GroupDescendingDescription".GetLocalizedResource();

		public bool IsOn
			=> context.GroupDirection is SortDirection.Descending;

		public bool IsExecutable
			=> context.GroupOption is not GroupOption.None;

		public GroupDescendingAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.GroupDirection = SortDirection.Descending;
			LayoutHelpers.UpdateOpenTabsPreferences();

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

	internal sealed class ToggleGroupDirectionAction : IAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "ToggleSortDirection".GetLocalizedResource();

		public string Description
			=> "ToggleGroupDirectionDescription".GetLocalizedResource();

		public ToggleGroupDirectionAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.GroupDirection = context.SortDirection is SortDirection.Descending ? SortDirection.Ascending : SortDirection.Descending;
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}
	}

	internal sealed partial class GroupByYearAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "Year".GetLocalizedResource();

		public string Description
			=> "GroupByYearDescription".GetLocalizedResource();

		public bool IsOn
			=> context.GroupByDateUnit is GroupByDateUnit.Year;

		public bool IsExecutable
			=> context.GroupOption.IsGroupByDate();

		public GroupByYearAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.GroupByDateUnit = GroupByDateUnit.Year;
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IDisplayPageContext.GroupOption):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IDisplayPageContext.GroupByDateUnit):
					OnPropertyChanged(nameof(IsOn));
					break;
			}
		}
	}

	internal sealed partial class GroupByMonthAction : ObservableObject, IToggleAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "Month".GetLocalizedResource();

		public string Description
			=> "GroupByMonthDescription".GetLocalizedResource();

		public bool IsOn
			=> context.GroupByDateUnit is GroupByDateUnit.Month;

		public bool IsExecutable
			=> context.GroupOption.IsGroupByDate();

		public GroupByMonthAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.GroupByDateUnit = GroupByDateUnit.Month;
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IDisplayPageContext.GroupOption):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IDisplayPageContext.GroupByDateUnit):
					OnPropertyChanged(nameof(IsOn));
					break;
			}
		}
	}

	internal sealed class ToggleGroupByDateUnitAction : IAction
	{
		private readonly IDisplayPageContext context;

		public string Label
			=> "ToggleGroupingUnit".GetLocalizedResource();

		public string Description
			=> "ToggleGroupByDateUnitDescription".GetLocalizedResource();

		public ToggleGroupByDateUnitAction()
		{
			context = Ioc.Default.GetRequiredService<IDisplayPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.GroupByDateUnit = context.GroupByDateUnit switch
			{
				GroupByDateUnit.Year => GroupByDateUnit.Month,
				GroupByDateUnit.Month => GroupByDateUnit.Day,
				_ => GroupByDateUnit.Year
			};
			LayoutHelpers.UpdateOpenTabsPreferences();

			return Task.CompletedTask;
		}
	}
}
