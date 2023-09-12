﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.TabBar;
using System.ComponentModel;

namespace Files.App.Data.Contexts
{
	public interface IMultitaskingContext : INotifyPropertyChanged
	{
		ITabBar? Control { get; }

		ushort TabCount { get; }

		TabBarItem CurrentTabItem { get; }
		ushort CurrentTabIndex { get; }

		TabBarItem SelectedTabItem { get; }
		ushort SelectedTabIndex { get; }
	}
}
