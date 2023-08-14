// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabView
{
	public sealed partial class TabViewItemControl : UserControl, ITabViewItemContainer, IDisposable
	{
		public event EventHandler<TabItemArguments> ContentChanged;

		public ITabViewItemContent TabItemContent
			=> ContentFrame?.Content as ITabViewItemContent;

		private TabItemArguments navigationArguments;
		public TabItemArguments NavigationArguments
		{
			get => navigationArguments;
			set
			{
				if (value != navigationArguments)
				{
					navigationArguments = value;
					if (navigationArguments is not null)
					{
						ContentFrame.Navigate(navigationArguments.InitialPageType, navigationArguments.NavigationArg);
					}
					else
					{
						ContentFrame.Content = null;
					}
				}
			}
		}

		public void Dispose()
		{
			if (TabItemContent is IDisposable disposableContent)
			{
				disposableContent?.Dispose();
			}
			ContentFrame.Content = null;
		}

		public TabViewItemControl()
		{
			InitializeComponent();
		}

		private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			if (TabItemContent is not null)
			{
				TabItemContent.ContentChanged += TabItemContent_ContentChanged;
			}
		}

		private void TabItemContent_ContentChanged(object sender, TabItemArguments e)
		{
			navigationArguments = e;
			ContentChanged?.Invoke(this, e);
		}
	}
}
