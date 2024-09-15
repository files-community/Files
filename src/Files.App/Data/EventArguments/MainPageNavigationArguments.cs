// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.EventArguments
{
	internal sealed class MainPageNavigationArguments
	{
		public object? Parameter { get; set; }

		public bool IgnoreStartupSettings { get; set; }
	}
}
