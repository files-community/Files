// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Actions
{
	internal abstract class BaseSetAsAction : ObservableObject, IAction
	{
		protected readonly IContentPageContext context;

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		public virtual bool IsExecutable =>
			context.ShellPage is not null &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder &&
			(context.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsCompatibleToSetAsWindowsWallpaper ?? false);

		public BaseSetAsAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public abstract Task ExecuteAsync(object? parameter = null);

		protected async void ShowErrorDialog(string message)
		{
			var errorDialog = new ContentDialog()
			{
				Title = "FailedToSetBackground".GetLocalizedResource(),
				Content = message,
				PrimaryButtonText = "OK".GetLocalizedResource(),
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				errorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			await errorDialog.TryShowAsync();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IContentPageContext.SelectedItem):
				case nameof(IContentPageContext.SelectedItems):
					{
						if (context.ShellPage is not null && context.ShellPage.SlimContentPage is not null)
						{
							var viewModel = context.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel;
							var extensions = context.SelectedItems.Select(selectedItem => selectedItem.FileExtension).Distinct().ToList();

							viewModel.CheckAllFileExtensions(extensions);
						}

						OnPropertyChanged(nameof(IsExecutable));
						break;
					}
			}
		}
	}
}
