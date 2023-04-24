// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.MultitaskingControl
{
	public interface ITabItemControl
	{
		string Header { get; }

		string Description { get; }

		IconSource IconSource { get; }

		TabItemControl Control { get; }

		bool AllowStorageItemDrop { get; }
	}
}