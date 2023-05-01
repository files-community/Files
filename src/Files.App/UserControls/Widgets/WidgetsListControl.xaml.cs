// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml.Controls;
using System;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.Widgets
{
	public sealed partial class WidgetsListControl : UserControl, IDisposable
	{
		public WidgetsListControlViewModel ViewModel
		{
			get => (WidgetsListControlViewModel)DataContext;
			set => DataContext = value;
		}

		public WidgetsListControl()
		{
			InitializeComponent();

			ViewModel = new WidgetsListControlViewModel();
		}

		public void Dispose()
		{
			ViewModel?.Dispose();
		}
	}
}