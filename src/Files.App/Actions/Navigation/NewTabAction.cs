// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;

namespace Files.App.Actions
{
	internal class NewTabAction : IAction
	{
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		public string Label { get; } = "NewTab".GetLocalizedResource();

		public string Description => "NewTabDescription".GetLocalizedResource();

		public HotKey HotKey { get; } = new(Keys.T, KeyModifiers.Ctrl);

		public Task ExecuteAsync() => mainPageViewModel.AddNewTabAsync();
	}
}
