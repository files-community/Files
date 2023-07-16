// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.LayoutModes
{
	/// <summary>
	/// Represents the browser page of Grid View
	/// </summary>
	public sealed partial class GridViewBrowser : BaseLayout
	{
		private readonly GridViewBrowserViewModel ViewModel;

		private readonly ICommandManager Commands;

		public GridViewBrowser() : base()
		{
			// Dependency injection
			ViewModel = Ioc.Default.GetRequiredService<GridViewBrowserViewModel>();
			Commands = Ioc.Default.GetRequiredService<ICommandManager>();
			BaseViewModel = ViewModel;
		}
	}
}
