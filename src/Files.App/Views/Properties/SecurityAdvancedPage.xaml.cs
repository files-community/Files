// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Data.Items;
using Files.App.Data.Parameters;
using Files.App.Dialogs;
using Files.App.Utils;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
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

		private async void AdvancedPermissionListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Skip opening selected items if the double tap doesn't capture an item
			if ((e.OriginalSource as FrameworkElement)?.DataContext is not ListedItem ||
				SecurityAdvancedViewModel.SelectedAccessControlEntry is null)
				return;

			var modifiableItem = new Utils.Storage.Security.AccessControlEntryModifiable(SecurityAdvancedViewModel.SelectedAccessControlEntry);

			// Show the Dialog
			var dialog = new PrincipalAccessControlEditorDialog()
			{
				XamlRoot = Window.Content.XamlRoot,
				ModifiableModel = modifiableItem,
			};

			var result = await dialog.ShowAsync();

			if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
			{
				// Save changes
				modifiableItem.SaveChanges();
			}
		}

		public async override Task<bool> SaveChangesAsync()
			=> await Task.FromResult(true);

		public override void Dispose()
		{
		}
	}
}
