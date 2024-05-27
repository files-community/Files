// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal sealed class FocusLeftPaneAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "FocusLeftPane".GetLocalizedResource();

		public string Description
			=> "FocusLeftPaneDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Left, KeyModifiers.CtrlShift);

		public bool IsExecutable
			=> context.IsMultiPaneActive;

		public FocusLeftPaneAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.ShellPage!.PaneHolder.FocusLeftPane();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
