// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.TabView;
using System.ComponentModel;

namespace Files.App.Data.Contexts
{
	public interface IMultitaskingContext : INotifyPropertyChanged
	{
		ITabView? Control { get; }

		ushort TabCount { get; }

		TabViewItem CurrentTabItem { get; }
		ushort CurrentTabIndex { get; }

		TabViewItem SelectedTabItem { get; }
		ushort SelectedTabIndex { get; }
	}
}
