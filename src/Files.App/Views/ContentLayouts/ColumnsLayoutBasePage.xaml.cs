// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.ContentLayouts
{
	/// <summary>
	/// Represents the base page of Column View
	/// </summary>
	public sealed partial class ColumnsLayoutBasePage : BaseLayout
	{
		private readonly ColumnBaseLayoutViewModel ViewModel;

		public ColumnsLayoutBasePage() : base()
		{
			// Dependency injection
			ViewModel = Ioc.Default.GetRequiredService<ColumnBaseLayoutViewModel>();
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
