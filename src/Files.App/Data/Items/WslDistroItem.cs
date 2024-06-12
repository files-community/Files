// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	public sealed class WslDistroItem : ObservableObject, ISidebarItem
	{
		public string Text { get; set; }

		private string path;
		public string Path
		{
			get => path;
			set
			{
				path = value;
				ToolTip = Path.Contains('?', StringComparison.Ordinal) ? Text : Path;
			}
		}

		public SidebarItemKind ItemType
			=> SidebarItemKind.LinuxDistro;

		private Uri icon;
		public Uri Icon
		{
			get => icon;
			set
			{
				SetProperty(ref icon, value, nameof(Icon));
				OnPropertyChanged(nameof(IconSource));
			}
		}

		public SidebarSectionKind Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public object? Children => null;

		private object toolTip = "";
		public object ToolTip
		{
			get => toolTip;
			set
			{
				SetProperty(ref toolTip, value);
			}
		}

		public bool IsExpanded { get => false; set { } }

		public IconSource? IconSource
		{
			get => new BitmapIconSource()
			{
				UriSource = icon,
				ShowAsMonochrome = false,
			};
		}

		public int CompareTo(ISidebarItem other) => Text.CompareTo(other.Text);
	}
}
