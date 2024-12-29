// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.EventArguments
{
	public sealed class CurrentInstanceChangedEventArgs : EventArgs
	{
		public ITabBarItemContent CurrentInstance { get; set; }

		public List<ITabBarItemContent> PageInstances { get; set; }
	}
}
