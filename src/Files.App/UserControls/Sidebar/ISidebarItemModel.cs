// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.Sidebar
{
	public interface ISidebarItemModel : INotifyPropertyChanged
	{
		object? Children { get; }

		IconSource? IconSource { get; }

		bool IsExpanded { get; set; }

		string Text { get; }

		object ToolTip { get; }
	}
}
