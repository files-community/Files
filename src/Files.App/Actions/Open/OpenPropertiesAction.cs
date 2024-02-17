// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class OpenPropertiesAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		private ActionExecutableType ExecutableType { get; set; }

		public string Label
			=> "OpenProperties".GetLocalizedResource();

		public string Description
			=> "OpenPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconProperties");

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.Menu);

		public bool IsExecutable
			=> GetIsExecutable();

		public OpenPropertiesAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			switch (ExecutableType)
			{
				case ActionExecutableType.DisplayPageContext:
					{
						EventHandler<object> flyoutClosed = null!;
						App.LastOpenedFlyout!.Closed += flyoutClosed;

						flyoutClosed = (s, e) =>
						{
							App.LastOpenedFlyout!.Closed -= flyoutClosed;
							FilePropertiesHelpers.OpenPropertiesWindow(ContentPageContext.ShellPage!);
						};

						break;
					}
				case ActionExecutableType.HomePageContext:
					{
						var flyout = HomePageContext.ItemContextFlyoutMenu;
						EventHandler<object> flyoutClosed = null!;
						flyout!.Closed += flyoutClosed;

						flyoutClosed = (s, e) =>
						{
							flyout!.Closed -= flyoutClosed;
							FilePropertiesHelpers.OpenPropertiesWindow(HomePageContext.RightClickedItem!.Item!, ContentPageContext.ShellPage!);
						};

						break;
					}
			}

			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			var executableInDisplayPage =
				ContentPageContext.PageType is not ContentPageTypes.Home &&
				!(ContentPageContext.PageType is ContentPageTypes.SearchResults &&
				!ContentPageContext.HasSelection);

			if (executableInDisplayPage)
				ExecutableType = ActionExecutableType.DisplayPageContext;

			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked;

			if (executableInHomePage)
				ExecutableType = ActionExecutableType.HomePageContext;

			return executableInDisplayPage || executableInHomePage;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
