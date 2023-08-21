// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.CustomTabView;
using System.ComponentModel;

namespace Files.App.Data.Contexts
{
	public interface IMultitaskingContext : INotifyPropertyChanged
	{
		ICustomTabView? Control { get; }

		ushort TabCount { get; }

		CustomTabViewItem CurrentTabItem { get; }
		ushort CurrentTabIndex { get; }

		CustomTabViewItem SelectedTabItem { get; }
		ushort SelectedTabIndex { get; }
	}
}
