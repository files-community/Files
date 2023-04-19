using Files.App.Dialogs;
using Files.App.ViewModels.Properties;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using System.Diagnostics;

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
					await dialog.TryShowAsync();
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