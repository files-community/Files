// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class StatusCenter : UserControl
	{
		private readonly StatusCenterViewModel ViewModel;

		public StatusCenter()
		{
			InitializeComponent();

			ViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
		}
	}
}
