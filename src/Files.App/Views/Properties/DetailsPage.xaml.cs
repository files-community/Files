using Files.App.Dialogs;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
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
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				fileProps.GetSystemFileProperties();
				stopwatch.Stop();
				Debug.WriteLine(string.Format("System file properties were obtained in {0} milliseconds", stopwatch.ElapsedMilliseconds));
			}
		}

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;
			}
			return contentDialog;
		}

		public override async Task<bool> SaveChangesAsync()
		{
			while (true)
			{
				using DynamicDialog dialog = DynamicDialogFactory.GetFor_PropertySaveErrorDialog();
				try
				{
					if (BaseProperties is FileProperties fileProps)
					{
						await fileProps.SyncPropertyChangesAsync();
					}
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

		private async void ClearPropertiesConfirmation_Click(object sender, RoutedEventArgs e)
		{
			ClearPropertiesFlyout.Hide();
			if (BaseProperties is FileProperties fileProps)
			{
				await fileProps.ClearPropertiesAsync();
			}
		}

		public override void Dispose()
		{
		}
	}
}