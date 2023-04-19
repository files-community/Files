namespace Files.App.UserControls.Widgets
{
	public abstract class WidgetCardItem : ObservableObject
	{
		public virtual string Path { get; set; }

		public virtual object Item { get; set; }
	}
}
