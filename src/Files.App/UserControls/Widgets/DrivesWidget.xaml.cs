// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Widgets;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class DrivesWidget : UserControl
	{
		public DrivesWidgetViewModel ViewModel { get; set; } = new();

		public DrivesWidget()
		{
			InitializeComponent();
		}
	}
}
