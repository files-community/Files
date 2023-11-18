// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.Widgets;

namespace Files.App.Actions
{
	internal class OpenDirectoryInNewTabAction : ObservableObject, IExtendedAction
	{
		private readonly IContentPageContext context;

		private readonly IUserSettingsService userSettingsService;

		private readonly MainPageViewModel _mainPageViewModel;

		public string Label
			=> "OpenInNewTab".GetLocalizedResource();

		public string Description
			=> "OpenDirectoryInNewTabDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenInNewTab");

		public bool IsExecutable
			=> GetIsExecutable();

		public object? Parameter { get; set; }

		public OpenDirectoryInNewTabAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			_mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (Parameter is not null && Parameter is WidgetCardItem item)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(
					async () =>
					{
						await _mainPageViewModel.AddNewTabByPathAsync(
							typeof(PaneHolderPage),
							item.Path);
					},
					Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);

				return;
			}

			if (context.ShellPage?.SlimContentPage?.SelectedItems is null)
				return;

			// Execute with selection
			foreach (ListedItem listedItem in context.ShellPage.SlimContentPage.SelectedItems)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(
					async () =>
					{
						await _mainPageViewModel.AddNewTabByPathAsync(
							typeof(PaneHolderPage),
							(listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
					},
					Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
			}
		}

		public bool GetIsExecutable()
		{
			return
				context.ShellPage is not null &&
				((context.ShellPage.SlimContentPage is not null &&
				context.SelectedItems.Count <= 5 &&
				context.SelectedItems.Where(x => x.IsFolder == true).Count() == context.SelectedItems.Count) ||
				Parameter is not null) &&
				userSettingsService.GeneralSettingsService.ShowOpenInNewTab;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
