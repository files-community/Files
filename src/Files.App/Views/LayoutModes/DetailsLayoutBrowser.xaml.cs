// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.LayoutModes
{
	/// <summary>
	/// Represents the browser page of Details View
	/// </summary>
	public sealed partial class DetailsLayoutBrowser : BaseLayout
	{
		private readonly DetailsLayoutBrowserViewModel ViewModel;

		private readonly ICommandManager Commands;

		public DetailsLayoutBrowser() : base()
		{
			// Dependency injection
			ViewModel = Ioc.Default.GetRequiredService<DetailsLayoutBrowserViewModel>();
			Commands = Ioc.Default.GetRequiredService<ICommandManager>();
			BaseViewModel = ViewModel;
		}
	}
}
