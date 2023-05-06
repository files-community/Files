// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Views;

namespace Files.App.Actions
{
	internal class DuplicateSelectedTabAction : IAction
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		public string Label { get; } = "DuplicateTab".GetLocalizedResource();
		public string Description => "DuplicateSelectedTabDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.K, KeyModifiers.CtrlShift);

		public async Task ExecuteAsync()
		{
			var arguments = context.SelectedTabItem.TabItemArguments;
			if (arguments is null)
			{
				await mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			}
			else
			{
				await mainPageViewModel.AddNewTabByParam(arguments.InitialPageType, arguments.NavigationArg, context.SelectedTabIndex + 1);
			}
		}
	}
}
