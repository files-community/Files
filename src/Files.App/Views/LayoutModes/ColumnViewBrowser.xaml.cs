// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.LayoutModes
{
	/// <summary>
	/// Represents the browser page of Column View
	/// </summary>
	public sealed partial class ColumnViewBrowser : BaseLayout
	{
		private readonly ColumnViewBrowserViewModel ViewModel;

		public ColumnViewBrowser() : base()
		{
			// Dependency injection
			ViewModel = Ioc.Default.GetRequiredService<ColumnViewBrowserViewModel>();
			BaseViewModel = ViewModel;
		}
	}
}
