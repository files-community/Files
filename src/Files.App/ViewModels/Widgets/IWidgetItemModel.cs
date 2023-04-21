using Microsoft.UI.Xaml.Controls;

namespace Files.App.ViewModels.Widgets
{
	public interface IWidgetItemModel : IDisposable
	{
		string WidgetName { get; }

		string WidgetHeader { get; }

		string AutomationProperties { get; }

		bool IsWidgetSettingEnabled { get; }

		bool ShowMenuFlyout { get; }

		MenuFlyoutItem MenuFlyoutItem { get; }

		Task RefreshWidget();
	}
}
