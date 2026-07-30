// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	public sealed partial class WslDistroItem : ObservableObject, INavigationControlItem
	{
		public required string Text { get; set; }

		private string path = string.Empty;
		public required string Path
		{
			get => path;
			set
			{
				path = value;
				ToolTip = Path.Contains('?', StringComparison.Ordinal) ? Text : Path;
			}
		}

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.LinuxDistro;

		private Uri? icon;
		public required Uri Icon
		{
			get => icon ?? throw new InvalidOperationException("The distribution icon has not been initialized.");
			set
			{
				SetProperty(ref icon, value, nameof(Icon));
				OnPropertyChanged(nameof(IconElement));
			}
		}

		public SectionType Section { get; set; }

		public required ContextMenuOptions MenuOptions { get; set; }

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

		public IconElement? IconElement
		{
			get
			{
				var source = new BitmapIconSource()
				{
					UriSource = icon,
					ShowAsMonochrome = false,
				};
				return source.CreateIconElement();
			}
		}

		FrameworkElement? ISidebarItemModel.IconElement => IconElement;
		FrameworkElement? ISidebarItemModel.ItemDecorator => null;

		public int CompareTo(INavigationControlItem? other) => other is null ? 1 : Text.CompareTo(other.Text);
	}
}
