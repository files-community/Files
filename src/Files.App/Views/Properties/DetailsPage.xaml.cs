// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Files.App.Views.Properties
{
	public sealed partial class DetailsPage : BasePropertiesPage
	{
		public DetailsPage()
		{
			InitializeComponent();
		}

		protected override async void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			base.Properties_Loaded(sender, e);

			if (BaseProperties is IFileProperties fileProps)
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				await fileProps.GetSystemFilePropertiesAsync();
				stopwatch.Stop();
				Debug.WriteLine(string.Format("System file properties were obtained in {0} milliseconds", stopwatch.ElapsedMilliseconds));

				ViewModel.IsPropertiesLoaded = true;
			}
		}

		public override async Task<bool> SaveChangesAsync()
		{
			while (true)
			{
				using DynamicDialog dialog = DynamicDialogFactory.GetFor_PropertySaveErrorDialog();
				try
				{
					if (BaseProperties is IFileProperties fileProps)
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
			if (BaseProperties is IFileProperties fileProps)
			{
				await fileProps.ClearPropertiesAsync();
			}
		}

		public override void Dispose()
		{
		}
	}
}
