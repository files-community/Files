// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System;

namespace Files.App.UserControls.MultitaskingControl
{
	public sealed partial class TabItemControl : UserControl, ITabItemContainer, IDisposable
	{
		public event EventHandler<TabItemArguments> ContentChanged;

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

		public ITabItemContent TabItemContent => ContentFrame?.Content as ITabItemContent;

		public TabItemControl()
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