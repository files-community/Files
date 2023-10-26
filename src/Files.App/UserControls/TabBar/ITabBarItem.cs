// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabBar
{
	/// <summary>
	/// Represents an interface for <see cref="TabBarItem"/>.
	/// </summary>
	public interface ITabBarItem
	{
		IconSource IconSource { get; }

		string Header { get; }

		string Description { get; }

		bool AllowStorageItemDrop { get; }

		public CustomTabViewItemParameter NavigationParameter { get; }
	}
}
