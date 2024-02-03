// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class DuplicateSelectedTabAction : IAction
	{
		private IMultitaskingContext MultitaskingContext { get; } = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label
			=> "DuplicateTab".GetLocalizedResource();

		public string Description
			=> "DuplicateSelectedTabDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.K, KeyModifiers.CtrlShift);

		public DuplicateSelectedTabAction()
		{
		}

		public async Task ExecuteAsync()
		{
			var arguments = MultitaskingContext.SelectedTabItem.NavigationParameter;

			if (arguments is null)
			{
				await NavigationHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			}
			else
			{
				await NavigationHelpers.AddNewTabByParamAsync(arguments.InitialPageType, arguments.NavigationParameter, MultitaskingContext.SelectedTabIndex + 1);
			}
		}
	}
}
