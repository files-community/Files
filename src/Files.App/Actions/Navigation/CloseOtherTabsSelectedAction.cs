// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to close tabs but selected ones.
	/// </summary>
	internal sealed class CloseOtherTabsSelectedAction : CloseTabBaseAction
	{
		public override string Label
			=> "CloseOtherTabs".GetLocalizedResource();

		public override string Description
			=> "CloseOtherTabsSelectedDescription".GetLocalizedResource();

		public CloseOtherTabsSelectedAction()
		{
		}

		public override Task ExecuteAsync()
		{
			MultitaskingTabsHelpers.CloseOtherTabs(context.SelectedTabItem, context.Control!);

			return Task.CompletedTask;
		}
	}
}
