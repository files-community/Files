// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	public sealed partial class WslDistroItem : ObservableObject, INavigationControlItem
	{
		public string? Text { get; set; }

		private string? path;
		public string? Path
		{
			get => path;
			set
			{
				path = value;
				ArgumentNullException.ThrowIfNull(value);
				var currentPath = value;
				ToolTip = currentPath.Contains('?', StringComparison.Ordinal) ? Text : currentPath;
			}
		}

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.LinuxDistro;

		private Uri? icon;
		public Uri? Icon
		{
			get => icon;
			set
			{
				SetProperty(ref icon, value, nameof(Icon));
				OnPropertyChanged(nameof(IconElement));
			}
		}

		public SectionType Section { get; set; }

		public ContextMenuOptions? MenuOptions { get; set; }

		public object? Children => null;

		private object? toolTip = "";
		public object? ToolTip
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

		FrameworkElement? ISidebarItemPresentationModel.IconElement => IconElement;
		FrameworkElement? ISidebarItemPresentationModel.ItemDecorator => null;

		public int CompareTo(INavigationControlItem? other)
		{
			var text = Text ?? throw new InvalidOperationException("The distribution name has not been initialized.");
			var otherText = other?.Text
				?? throw new ArgumentException("The compared item must have a name.", nameof(other));

			return text.CompareTo(otherText);
		}
	}
}
