// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class DuplicateSelectedTabAction : IAction
	{
		private readonly IMultitaskingContext context;

		private readonly MainPageViewModel mainPageViewModel;

		public string Label
			=> "DuplicateTab".GetLocalizedResource();

		public string Description
			=> "DuplicateSelectedTabDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.K, KeyModifiers.CtrlShift);

		public DuplicateSelectedTabAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();
			mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();
		}

		public async Task ExecuteAsync()
		{
			var arguments = context.SelectedTabItem.NavigationParameter;
			if (arguments is null)
				await mainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			else
				await mainPageViewModel.AddNewTabByParamAsync(arguments.InitialPageType, arguments.NavigationParameter, context.SelectedTabIndex + 1);
		}
	}
}
