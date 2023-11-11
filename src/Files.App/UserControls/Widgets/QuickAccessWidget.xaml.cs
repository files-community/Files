// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class QuickAccessWidget : UserControl
	{
		private QuickAccessWidgetViewModel ViewModel { get; } = Ioc.Default.GetRequiredService<QuickAccessWidgetViewModel>();

		public QuickAccessWidget()
		{
			InitializeComponent();
		}
	}
}
