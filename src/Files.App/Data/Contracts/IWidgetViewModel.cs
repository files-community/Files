// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Contracts
{
	public interface IWidgetViewModel : IDisposable
	{
		public string WidgetName { get; }

		public string WidgetHeader { get; }

		public string AutomationProperties { get; }

		public bool IsWidgetSettingEnabled { get; }

		public bool ShowMenuFlyout { get; }

		public MenuFlyoutItem? MenuFlyoutItem { get; }

		public Task RefreshWidgetAsync();
	}
}
