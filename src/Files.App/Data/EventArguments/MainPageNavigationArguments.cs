// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.EventArguments
{
	internal sealed class MainPageNavigationArguments
	{
		public object? Parameter { get; set; }

		public bool IgnoreStartupSettings { get; set; }
	}
}
