// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabView
{
	public class TabViewItem : ObservableObject, ITabViewItem, ITabItemControl, IDisposable
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

		private TabItemArguments _TabItemArguments;
		public TabItemArguments TabItemArguments
		{
			get => Control?.NavigationArguments ?? _TabItemArguments;
		}

		public TabViewItemControl Control { get; private set; }

		public TabViewItem()
		{
			Control = new TabViewItemControl();
		}

		public void Unload()
		{
			MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

			Control.ContentChanged -= mainPageViewModel.Control_ContentChanged;

			_TabItemArguments = Control?.NavigationArguments;

			Dispose();
		}

		public void Dispose()
		{
			Control?.Dispose();
			Control = null;
		}
	}
}
