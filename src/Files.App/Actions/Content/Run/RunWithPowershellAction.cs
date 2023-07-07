// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Utils.Shell;

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
			Win32API.RunPowershellCommand($"{context.ShellPage?.SlimContentPage?.SelectedItem.ItemPath}", false);

			return Task.CompletedTask;
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
