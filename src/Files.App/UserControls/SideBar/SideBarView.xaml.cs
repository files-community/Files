// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Runtime.CompilerServices;

namespace Files.App.UserControls.SideBar
{
	[ContentProperty(Name = "InnerContent")]
	public sealed partial class SideBarView : UserControl, INotifyPropertyChanged
	{
		private bool canOpenInNewPane;

		public bool CanOpenInNewPane
		{
			get => canOpenInNewPane;
			set
			{
				if (value != canOpenInNewPane)
				{
					canOpenInNewPane = value;
					NotifyPropertyChanged(nameof(CanOpenInNewPane));
				}
			}
		}

		public SideBarView()
		{
			InitializeComponent();
		}


		public event PropertyChangedEventHandler? PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private async void SideBar_ItemInvoked(object sender, object item)
		{
			ViewModel.HandleItemInvoked(item);
		}

		private void SideBar_ItemContextInvoked(object sender, ItemContextInvokedArgs args)
		{
			ViewModel.HandleItemContextInvoked(sender, args);
		}

		private void SideBarView_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (args.NewSize.Width < 650)
			{
				DisplayMode = SideBarDisplayMode.Minimal;
			}
			else if (args.NewSize.Width < 1300)
			{
				DisplayMode = SideBarDisplayMode.Compact;
			}
			else
			{
				DisplayMode = SideBarDisplayMode.Expanded;
			}
		}

		private void TogglePaneButton_Click(object sender, RoutedEventArgs e)
		{
			if (DisplayMode == SideBarDisplayMode.Minimal)
			{
				IsPaneOpen = !IsPaneOpen;
			}
		}

		private async void SideBar_ItemDropped(object sender, ItemDroppedEventArgs e)
		{
			ViewModel?.HandleItemDropped(e);
		}

		private void SideBar_Loaded(object sender, RoutedEventArgs e)
		{
			(this.FindDescendant("TabContentBorder") as Border)!.Child = TabContent;
		}
	}
}
