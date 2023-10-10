// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.UserControls.Widgets;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class WidgetsListControl : UserControl, IDisposable
	{
		public WidgetsListControlViewModel ViewModel { get; set; }

		public WidgetsListControl()
		{
			InitializeComponent();

			ViewModel = new();
		}

		public void Dispose()
		{
			ViewModel?.Dispose();
		}
	}
}
