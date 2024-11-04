// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.IO;
using Windows.Foundation.Metadata;

namespace Files.App.Actions
{
	internal sealed class CreateAlternateDataStreamAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "CreateAlternateDataStream".GetLocalizedResource();

		public string Description
			=> "CreateAlternateDataStreamDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.URL");

		public override bool IsExecutable =>
			context.HasSelection &&
			context.CanCreateItem &&
			(context.ShellPage?.ShellViewModel.WorkingDirectory != Path.GetPathRoot(context.ShellPage?.ShellViewModel.WorkingDirectory)) &&
			UIHelpers.CanShowDialog;

		public CreateAlternateDataStreamAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var nameDialog = DynamicDialogFactory.GetFor_CreateAlternateDataStreamDialog();
			await nameDialog.TryShowAsync();

			if (nameDialog.DynamicResult != DynamicDialogResult.Primary)
				return;

			var userInput = nameDialog.ViewModel.AdditionalData as string;
			await Task.WhenAll(context.SelectedItems.Select(async selectedItem =>
			{
				var isDateOk = Win32Helper.GetFileDateModified(selectedItem.ItemPath, out var dateModified);
				var isReadOnly = Win32Helper.HasFileAttribute(selectedItem.ItemPath, System.IO.FileAttributes.ReadOnly);

				// Unset read-only attribute (#7534)
				if (isReadOnly)
					Win32Helper.UnsetFileAttribute(selectedItem.ItemPath, System.IO.FileAttributes.ReadOnly);

				if (!Win32Helper.WriteStringToFile($"{selectedItem.ItemPath}:{userInput}", ""))
				{
					var dialog = new ContentDialog
					{
						Title = Strings.ErrorCreatingDataStreamTitle.GetLocalizedResource(),
						Content = Strings.ErrorCreatingDataStreamDescription.GetLocalizedResource(),
						PrimaryButtonText = "Ok".GetLocalizedResource()
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await dialog.TryShowAsync();
				}

				// Restore read-only attribute (#7534)
				if (isReadOnly)
					Win32Helper.SetFileAttribute(selectedItem.ItemPath, System.IO.FileAttributes.ReadOnly);

				// Restore date modified
				if (isDateOk)
					Win32Helper.SetFileDateModified(selectedItem.ItemPath, dateModified);
			}));
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.CanCreateItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
