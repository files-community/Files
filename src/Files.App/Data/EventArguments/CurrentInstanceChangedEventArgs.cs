// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public class CurrentInstanceChangedEventArgs : EventArgs
	{
		public ITabViewItemContent CurrentInstance { get; set; }

		public List<ITabViewItemContent> PageInstances { get; set; }
	}
}
