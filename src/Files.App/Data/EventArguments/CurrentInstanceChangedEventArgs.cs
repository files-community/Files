// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.EventArguments
{
	public class CurrentInstanceChangedEventArgs : EventArgs
	{
		public ICustomTabViewItemContent CurrentInstance { get; set; }

		public List<ICustomTabViewItemContent> PageInstances { get; set; }
	}
}
