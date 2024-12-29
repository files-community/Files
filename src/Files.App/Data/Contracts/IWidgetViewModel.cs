// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Contracts
{
	public interface IWidgetViewModel : IDisposable
	{
		string WidgetName { get; }

		string WidgetHeader { get; }

		string AutomationProperties { get; }

		bool IsWidgetSettingEnabled { get; }

		bool ShowMenuFlyout { get; }

		MenuFlyoutItem? MenuFlyoutItem { get; }

		Task RefreshWidgetAsync();
	}
}
