namespace Files.App.UserControls.Sidebar
{
	public interface ISidebarViewModel
	{
		public BulkConcurrentObservableCollection<INavigationControlItem> SidebarItems { get; }

		public void HandleItemDropped(ItemDroppedEventArgs args);

		public void HandleItemContextInvoked(object sender, ItemContextInvokedArgs args);

		public void HandleItemInvoked(object item);
	}
}
