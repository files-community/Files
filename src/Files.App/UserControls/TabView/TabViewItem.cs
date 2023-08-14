// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using System.Text.Json;

namespace Files.App.UserControls.TabView
{
	public class TabViewItem : ObservableObject, ITabViewItem, ITabItemControl, IDisposable
	{
		private readonly MainPageViewModel mainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		private string header;
		public string Header
		{
			get => header;
			set => SetProperty(ref header, value);
		}

		private string description = null;
		public string Description
		{
			get => description;
			set => SetProperty(ref description, value);
		}

		private string toolTipText;
		public string ToolTipText
		{
			get => toolTipText;
			set => SetProperty(ref toolTipText, value);
		}

		private IconSource iconSource;
		public IconSource IconSource
		{
			get => iconSource;
			set => SetProperty(ref iconSource, value);
		}

		public TabViewItemControl Control { get; private set; }

		private bool allowStorageItemDrop;
		public bool AllowStorageItemDrop
		{
			get => allowStorageItemDrop;
			set => SetProperty(ref allowStorageItemDrop, value);
		}

		private TabItemArguments tabItemArguments;
		public TabItemArguments TabItemArguments
		{
			get => Control?.NavigationArguments ?? tabItemArguments;
		}

		public TabViewItem()
		{
			Control = new TabViewItemControl();
		}

		public void Unload()
		{
			Control.ContentChanged -= mainPageViewModel.Control_ContentChanged;
			tabItemArguments = Control?.NavigationArguments;
			Dispose();
		}

		public void Dispose()
		{
			Control?.Dispose();
			Control = null;
		}
	}
}
