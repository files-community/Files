// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class CloseTabsToTheRightSelectedAction : CloseTabBaseAction
	{
		public override string Label
			=> Strings.CloseTabsToTheRight.GetLocalizedResource();

		public override string Description
			=> Strings.CloseTabsToTheRightSelectedDescription.GetLocalizedResource();

		public CloseTabsToTheRightSelectedAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedTabItem is { } selectedTabItem && context.Control is { } control)
				MultitaskingTabsHelpers.CloseTabsToTheRight(selectedTabItem, control);

			return Task.CompletedTask;
		}

		protected override bool GetIsExecutable()
		{
			return context.Control is not null && context.SelectedTabIndex < context.TabCount - 1;
		}

		protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.TabCount):
				case nameof(IMultitaskingContext.SelectedTabIndex):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
