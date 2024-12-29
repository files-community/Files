// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

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

		public TabBarItemParameter NavigationParameter { get; }
	}
}
