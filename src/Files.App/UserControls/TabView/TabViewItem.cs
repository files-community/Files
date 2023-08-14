// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabView
{
	public class TabViewItem : ObservableObject, ITabViewItem, IDisposable
	{
		private IconSource _IconSource;
		public IconSource IconSource
		{
			get => _IconSource;
			set => SetProperty(ref _IconSource, value);
		}

		private string _Header;
		public string Header
		{
			get => _Header;
			set => SetProperty(ref _Header, value);
		}

		private string _Description = null;
		public string Description
		{
			get => _Description;
			set => SetProperty(ref _Description, value);
		}

		private string _ToolTipText;
		public string ToolTipText
		{
			get => _ToolTipText;
			set => SetProperty(ref _ToolTipText, value);
		}

		private bool _AllowStorageItemDrop;
		public bool AllowStorageItemDrop
		{
			get => _AllowStorageItemDrop;
			set => SetProperty(ref _AllowStorageItemDrop, value);
		}

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

		public Frame ContentFrame { get; private set; }

		public event EventHandler<TabItemArguments> ContentChanged;

		public ITabViewItemContent TabItemContent
			=> ContentFrame?.Content as ITabViewItemContent;

		public TabViewItem()
		{
			ContentFrame = new()
			{
				CacheSize = 0,
				IsNavigationStackEnabled = false,
			};

			ContentFrame.Navigated += ContentFrame_Navigated;
		}

		public void Unload()
		{
			MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

			ContentChanged -= mainPageViewModel.Control_ContentChanged;

			Dispose();
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
