namespace Files.App.UserControls.SideBar
{
	public interface ISideBarViewModel
	{
		public BulkConcurrentObservableCollection<INavigationControlItem> SideBarItems { get; }

		public void HandleItemDropped(ItemDroppedEventArgs args);

		public void HandleItemContextInvoked(object sender, ItemContextInvokedArgs args);

		public void HandleItemInvoked(object item);
	}
}
