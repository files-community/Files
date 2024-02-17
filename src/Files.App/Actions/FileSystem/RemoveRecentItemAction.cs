// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class RemoveRecentItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		public string Label
			=> "RecentItemRemove/Text".GetLocalizedResource();

		public string Description
			=> "RecentItemRemove/Text".GetLocalizedResource();

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
				if (HomePageContext.RightClickedItem is WidgetRecentItem item)
					await App.RecentItemsManager.UnpinFromRecentFiles(item);
			}
			catch (Exception) { }
		}

		private bool GetIsExecutable()
		{
			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked &&
				HomePageContext.RightClickedItem is WidgetRecentItem;

			return executableInHomePage;
		}

		public void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
