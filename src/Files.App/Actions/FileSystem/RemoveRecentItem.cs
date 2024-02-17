// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class RemoveRecentItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		public string Label
			=> "RemoveRecentItemText".GetLocalizedResource();

		public string Description
			=> "RemoveRecentItemDescription".GetLocalizedResource();

		public bool IsExecutable
			=> GetIsExecutable();

		public RemoveRecentItemAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			try
			{
				if (HomePageContext.RightClickedItem?.Item is RecentItem item)
					await App.RecentItemsManager.UnpinFromRecentFiles(item);
			}
			catch (Exception) { }
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
