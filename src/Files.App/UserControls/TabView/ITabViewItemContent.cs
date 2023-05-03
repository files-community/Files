// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabView
{
	public interface ITabItemControl
	{
		string Header { get; }

		string Description { get; }

		IconSource IconSource { get; }

		TabViewItemContent Control { get; }

		bool AllowStorageItemDrop { get; }
	}
}
