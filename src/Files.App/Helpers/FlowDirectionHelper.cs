using Microsoft.UI.Xaml;

namespace Files.App.Helpers
{
	public static class FlowDirectionHelper
	{
		public static bool GetForceLeftToRight(DependencyObject obj)
			=> (bool)obj.GetValue(ForceLeftToRightProperty);

		public static void SetForceLeftToRight(DependencyObject obj, bool value)
			=> obj.SetValue(ForceLeftToRightProperty, value);

		public static readonly DependencyProperty ForceLeftToRightProperty =
			DependencyProperty.RegisterAttached(
				"ForceLeftToRight",
				typeof(bool),
				typeof(FlowDirectionHelper),
				new PropertyMetadata(false, OnForceLeftToRightChanged));

		private static void OnForceLeftToRightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is FrameworkElement element)
			{
				// Pokud je ForceLeftToRight true, nastavíme FlowDirection na LeftToRight
				if ((bool)e.NewValue)
				{
					element.FlowDirection = FlowDirection.LeftToRight;

					// Přidáme event handler pro změny FlowDirection
					_ = element.RegisterPropertyChangedCallback(
						FrameworkElement.FlowDirectionProperty,
						(sender, dp) =>
						{
							if (GetForceLeftToRight(element))
							{
								element.FlowDirection = FlowDirection.LeftToRight;
							}
						});
				}
			}
		}
	}
}
