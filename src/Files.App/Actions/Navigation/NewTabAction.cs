// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class NewTabAction : IAction
	{
		public string Label
			=> "NewTab".GetLocalizedResource();

		public string Description
			=> "NewTabDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.T, KeyModifiers.Ctrl);

		public NewTabAction()
		{
		}

		public Task ExecuteAsync()
		{
			return NavigationHelpers.AddNewTabAsync();
		}
	}
}
