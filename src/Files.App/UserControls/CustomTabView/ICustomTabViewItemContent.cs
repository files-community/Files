// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.UserControls.CustomTabView
{
	/// <summary>
	/// Represents content item for <see cref="CustomTabViewItem"/>.
	/// </summary>
	public interface ICustomTabViewItemContent
	{
		public bool IsCurrentInstance { get; set; }

		public CustomTabViewItemParameter TabItemParameter { get; }

		public event EventHandler<CustomTabViewItemParameter> ContentChanged;

		public Task TabItemDragOver(object sender, DragEventArgs e);

		public Task TabItemDrop(object sender, DragEventArgs e);
	}
}
