// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class RunWithPowershellAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "RunWithPowerShell".GetLocalizedResource();

		public string Description
			=> "RunWithPowershellDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE756");

		public bool IsExecutable =>
			context.SelectedItem is not null &&
			FileExtensionHelpers.IsPowerShellFile(context.SelectedItem.FileExtension);

		public RunWithPowershellAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return Win32API.RunPowershellCommandAsync($"{context.ShellPage?.SlimContentPage?.SelectedItem?.ItemPath}", false);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
