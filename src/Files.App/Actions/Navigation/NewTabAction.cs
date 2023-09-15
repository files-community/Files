// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents action to open a new tab.
	/// </summary>
	internal class NewTabAction : IAction
	{
		private readonly MainPageViewModel mainPageViewModel;

		public string Label
			=> "NewTab".GetLocalizedResource();

		public string Description
			=> "NewTabDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.T, KeyModifiers.Ctrl);

		public NewTabAction()
		{
			mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();
		}

		public Task ExecuteAsync()
		{
			return mainPageViewModel.AddNewTabAsync();
		}
	}
}
