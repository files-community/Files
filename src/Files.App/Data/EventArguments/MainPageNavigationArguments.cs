// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	internal sealed class MainPageNavigationArguments
	{
		public object? Parameter { get; set; }

		public bool IgnoreStartupSettings { get; set; }
	}
}
