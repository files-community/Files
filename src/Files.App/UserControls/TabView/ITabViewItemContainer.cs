// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.UserControls.TabView
{
	public interface ITabViewItemContainer
	{
		public ITabViewItemContent TabItemContent { get; }

		public event EventHandler<TabItemArguments> ContentChanged;
	}
}
