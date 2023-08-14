// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.UserControls.TabView
{
	public interface ITabViewItemContent
	{
		public bool IsCurrentInstance { get; set; }

		public TabItemArguments TabItemArguments { get; }

		public event EventHandler<TabItemArguments> ContentChanged;

		public Task TabItemDragOver(object sender, DragEventArgs e);

		public Task TabItemDrop(object sender, DragEventArgs e);
	}
}
