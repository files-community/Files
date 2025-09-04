// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Actions
{
	[GeneratedRichCommand]
	internal sealed partial class NextTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext multitaskingContext;
		private readonly IContentPageContext contentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> Strings.NextTab.GetLocalizedResource();

		public string Description
			=> Strings.NextTabDescription.GetLocalizedResource();

		public bool IsExecutable
			=> multitaskingContext.TabCount > 1;

		public HotKey HotKey
			=> new(Keys.Tab, KeyModifiers.Ctrl);

		public NextTabAction()
		{
			multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			multitaskingContext.PropertyChanged += MultitaskingContext_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			App.AppModel.TabStripSelectedIndex = (App.AppModel.TabStripSelectedIndex + 1) % multitaskingContext.TabCount;

			// Small delay for the UI to load
			await Task.Delay(500);

			// Focus the content of the selected tab item (needed for keyboard navigation)
			contentPageContext.ShellPage!.PaneHolder.FocusActivePane();
		}

		private void MultitaskingContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMultitaskingContext.TabCount))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
