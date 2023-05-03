// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Commands;
using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class OpenPropertiesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "OpenProperties".GetLocalizedResource();

		public string Description => "OpenPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconProperties");

		public bool IsExecutable =>
			context.ShellPage.InstanceViewModel.IsPageTypeNotHome &&
			context.HasSelection;

		public HotKey HotKey { get; } = new(Keys.P, KeyModifiers.Ctrl);

		public OpenPropertiesAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			FilePropertiesHelpers.OpenPropertiesWindow(context.ShellPage);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage.InstanceViewModel.IsPageTypeNotHome):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
