// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabView
{
	public sealed partial class TabViewItemControl : UserControl, IDisposable
	{
		public event EventHandler<TabItemArguments> ContentChanged;

		public ITabViewItemContent TabItemContent
			=> ContentFrame?.Content as ITabViewItemContent;

		private TabItemArguments _NavigationArguments;
		public TabItemArguments NavigationArguments
		{
			get => _NavigationArguments;
			set
			{
				if (value != _NavigationArguments)
				{
					_NavigationArguments = value;
					if (_NavigationArguments is not null)
					{
						ContentFrame.Navigate(_NavigationArguments.InitialPageType, _NavigationArguments.NavigationArg);
					}
					else
					{
						ContentFrame.Content = null;
					}
				}
			}
		}

		public TabViewItemControl()
		{
			InitializeComponent();
		}

		private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			if (TabItemContent is not null)
				TabItemContent.ContentChanged += TabItemContent_ContentChanged;
		}

		private void TabItemContent_ContentChanged(object sender, TabItemArguments e)
		{
			_NavigationArguments = e;
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
