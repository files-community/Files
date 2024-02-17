// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class ClearRecentItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		public string Label
			=> "ClearRecentItemText".GetLocalizedResource();

		public string Description
			=> "ClearRecentItemDescription".GetLocalizedResource();

		public bool IsExecutable
			=> GetIsExecutable();

		public ClearRecentItemAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			try
			{
				App.RecentItemsManager.ClearRecentItems();
			}
			catch (Exception) { }

			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked &&
				HomePageContext.RightClickedItem is RecentItem;

			return executableInHomePage;
		}

		public void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
