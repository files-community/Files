using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.Shared.Enums;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Files.App.Dialogs;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;

namespace Files.App.ViewModels.Properties
{
	public class DetailsViewModel : ObservableObject
	{
		public IAsyncRelayCommand ClearPropertiesConfirmationCommand { get; set; }

		public DetailsViewModel()
		{
			ClearPropertiesConfirmationCommand = new AsyncRelayCommand<BaseProperties>(ExecuteClearPropertiesConfirmation);
		}

		private async Task ExecuteClearPropertiesConfirmation(BaseProperties? baseProperties)
		{
			if (baseProperties is FileProperties fileProps)
				await fileProps.ClearPropertiesAsync();
		}

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (FilePropertiesHelpers.IsWinUI3)
				contentDialog.XamlRoot = App.Window.Content.XamlRoot;

			return contentDialog;
		}

		public async Task<bool> SaveChanges()
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
	}
}
