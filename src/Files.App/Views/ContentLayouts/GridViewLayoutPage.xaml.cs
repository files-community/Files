// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.ContentLayouts
{
	/// <summary>
	/// Represents the browser page of Grid View
	/// </summary>
	public sealed partial class GridViewLayoutPage : BaseLayout
	{
		private readonly GridViewLayoutViewModel ViewModel;

		private readonly ICommandManager Commands;

		public GridViewLayoutPage() : base()
		{
			// Dependency injection
			ViewModel = Ioc.Default.GetRequiredService<GridViewLayoutViewModel>();
			Commands = Ioc.Default.GetRequiredService<ICommandManager>();
			BaseViewModel = ViewModel;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			// Add item jumping handler
			CharacterReceived += ViewModel.Page_CharacterReceived;

			ViewModel.OnNavigatedTo(e);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			// Remove item jumping handler
			CharacterReceived += ViewModel.Page_CharacterReceived;

			ViewModel.OnNavigatingFrom(e);
		}
	}
}
