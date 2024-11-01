// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class CloseAllTabsAction : CloseTabBaseAction
	{
		public override string Label
			=> Strings.CloseAllTabs.GetLocalizedResource();

		public override string Description
			=> Strings.CloseAllTabsDescription.GetLocalizedResource();

		public override HotKey HotKey
			=> new(Keys.W, KeyModifiers.CtrlShift);

		public override bool IsExecutable
			=> context.Control is not null;

		public CloseAllTabsAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.Control is not null)
				MultitaskingTabsHelpers.CloseAllTabs(context.Control);

			return Task.CompletedTask;
		}
	}
}
