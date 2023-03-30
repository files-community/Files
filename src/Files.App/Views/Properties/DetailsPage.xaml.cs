using Files.App.Dialogs;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class DetailsPage : BasePropertiesPage
	{
		public DetailsPage()
		{
			InitializeComponent();
		}

		protected override void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			base.Properties_Loaded(sender, e);

			if (BaseProperties is FileProperties fileProps)
				fileProps.GetSystemFileProperties();
		}

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Helpers.FilePropertiesHelpers.IsWinUI3)
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;

			return contentDialog;
		}

		private async void ClearPropertiesConfirmation_Click(object sender, RoutedEventArgs e)
		{
			ClearPropertiesFlyout.Hide();

			if (BaseProperties is FileProperties fileProps)
				await fileProps.ClearPropertiesAsync();
		}

		public override async Task<bool> SaveChangesAsync()
		{
			while (true)
			{
				using DynamicDialog dialog = DynamicDialogFactory.GetFor_PropertySaveErrorDialog();

				try
				{
					if (BaseProperties is FileProperties fileProps)
						await fileProps.SyncPropertyChangesAsync();

					return true;
				}
				catch
				{
					await SetContentDialogRoot(dialog).TryShowAsync();

					switch (dialog.DynamicResult)
					{
						case DynamicDialogResult.Primary:
							break;
						case DynamicDialogResult.Secondary:
							return true;
						case DynamicDialogResult.Cancel:
							return false;
					}
				}
			}
		}

		public override void Dispose()
		{
		}
	}
}