// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions
{
	internal sealed class FlattenFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> "FlattenFolder".GetLocalizedResource();

		public string Description
			=> "FlattenFolderDescription".GetLocalizedResource();

		public bool IsExecutable =>
			GeneralSettingsService.ShowFlattenOptions &&
			context.ShellPage is not null &&
			context.HasSelection &&
			context.SelectedItem?.PrimaryItemAttribute is StorageItemTypes.Folder;

		public FlattenFolderAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
			GeneralSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var optionsDialog = new ContentDialog()
			{
				Title = "FlattenFolderDialogTitle".GetLocalizedResource(),
				Content = "FlattenFolderDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "OK".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				optionsDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			return optionsDialog.TryShowAsync();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IGeneralSettingsService.ShowFlattenOptions))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
