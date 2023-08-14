// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabView
{
	public interface ITabViewItem
	{
		IconSource IconSource { get; }

		string Header { get; }

		string Description { get; }

		bool AllowStorageItemDrop { get; }

		public TabItemArguments NavigationArguments { get; }
	}
}
