// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public sealed class CurrentInstanceChangedEventArgs : EventArgs
	{
		public ITabBarItemContent CurrentInstance { get; set; }

		public List<ITabBarItemContent> PageInstances { get; set; }
	}
}
