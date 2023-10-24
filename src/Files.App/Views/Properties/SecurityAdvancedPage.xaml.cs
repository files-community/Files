// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
using Files.App.Data.Parameters;
using Files.App.Dialogs;
using Files.App.Utils;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityAdvancedPage : BasePropertiesPage
	{
		private AppWindow AppWindow;

		private Window Window;

		private SecurityAdvancedViewModel SecurityAdvancedViewModel { get; set; }

		public SecurityAdvancedPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var parameter = (PropertiesPageNavigationParameter)e.Parameter;

			AppWindow = parameter.AppWindow;
			Window = parameter.Window;

			SecurityAdvancedViewModel = new(parameter);

			base.OnNavigatedTo(e);
		}

		private async void AccessControlEntryItemRoot_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
			// Show the Dialog
			var dialog = new PrincipalAccessControlEditorDialog()
			{
				XamlRoot = Window.Content.XamlRoot,
			};

			var result = await dialog.ShowAsync();
		}

		public async override Task<bool> SaveChangesAsync()
			=> await Task.FromResult(true);

		public override void Dispose()
		{
		}
	}
}
