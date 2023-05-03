// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabView
{
	public class TabItem : ObservableObject, ITabItem, ITabItemControl, IDisposable
	{
		private MainPageViewModel MainPageViewModel { get; }

		public TabViewItemContent Control { get; private set; }

		private string? _Header;
		public string? Header
		{
			get => _Header;
			set => SetProperty(ref _Header, value);
		}

		private string? _Description = null;
		public string? Description
		{
			get => _Description;
			set => SetProperty(ref _Description, value);
		}

		private string? _ToolTipText;
		public string? ToolTipText
		{
			get => _ToolTipText;
			set => SetProperty(ref _ToolTipText, value);
		}

		private IconSource _IconSource;
		public IconSource IconSource
		{
			get => _IconSource;
			set => SetProperty(ref _IconSource, value);
		}

		private bool _AllowStorageItemDrop;
		public bool AllowStorageItemDrop
		{
			get => _AllowStorageItemDrop;
			set => SetProperty(ref _AllowStorageItemDrop, value);
		}

		private TabItemArguments _TabItemArguments;
		public TabItemArguments TabItemArguments
			=> Control.NavigationArguments ?? _TabItemArguments;

		public TabItem()
		{
			MainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

			Control = new TabViewItemContent();
		}

		public void Unload()
		{
			Control.ContentChanged -= MainPageViewModel.Control_ContentChanged;
			_TabItemArguments = Control.NavigationArguments;

			Dispose();
		}

		public void Dispose()
		{
			Control.Dispose();
		}
	}
}
