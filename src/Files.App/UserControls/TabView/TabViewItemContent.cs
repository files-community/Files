// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.UserControls.TabView
{
	public class TabViewItemContent : ITabItemContainer, IDisposable
	{
		public event EventHandler<TabItemArguments> ContentChanged;

		private Frame ContentFrame { get; }

		public ITabItemContent? TabItemContent
			=> ContentFrame?.Content as ITabItemContent;

		private TabItemArguments? _NavigationArguments;
		public TabItemArguments? NavigationArguments
		{
			get => _NavigationArguments;
			set
			{
				if (value != _NavigationArguments)
				{
					_NavigationArguments = value;

					if (_NavigationArguments is not null)
						ContentFrame.Navigate(_NavigationArguments.InitialPageType, _NavigationArguments.NavigationArg);
					else
						ContentFrame.Content = null;
				}
			}
		}

		public TabViewItemContent()
		{
			ContentFrame = new()
			{
				CacheSize = 0,
				IsNavigationStackEnabled = false
			};

			ContentFrame.Navigated += ContentFrame_Navigated;
		}

		private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
		{
			if (TabItemContent is not null)
				TabItemContent.ContentChanged += TabItemContent_ContentChanged;
		}

		private void TabItemContent_ContentChanged(object sender, TabItemArguments e)
		{
			NavigationArguments = e;
			ContentChanged?.Invoke(this, e);
		}

		public void Dispose()
		{
			if (TabItemContent is IDisposable disposableContent)
				disposableContent?.Dispose();

			ContentFrame.Content = null;
		}
	}
}
