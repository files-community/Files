using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.ViewModels.UserControls.Widgets
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
