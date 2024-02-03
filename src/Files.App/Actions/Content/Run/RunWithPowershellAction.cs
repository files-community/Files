// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal class RunWithPowershellAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "RunWithPowerShell".GetLocalizedResource();

		public string Description
			=> "RunWithPowershellDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE756");

		public bool IsExecutable =>
			ContentPageContext.SelectedItem is not null &&
			FileExtensionHelpers.IsPowerShellFile(ContentPageContext.SelectedItem.FileExtension);

		public RunWithPowershellAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			return Win32API.RunPowershellCommandAsync($"{ContentPageContext.ShellPage?.SlimContentPage?.SelectedItem?.ItemPath}", false);
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
