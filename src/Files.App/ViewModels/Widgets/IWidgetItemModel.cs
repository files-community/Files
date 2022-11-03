using System;
using System.Threading.Tasks;

namespace Files.App.ViewModels.Widgets
{
	public interface IWidgetItemModel : IDisposable
	{
		string WidgetName { get; }

		string WidgetHeader { get; }

		string AutomationProperties { get; }

		bool IsWidgetSettingEnabled { get; }

		Task RefreshWidget();
	}
}