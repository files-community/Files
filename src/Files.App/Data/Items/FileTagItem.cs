// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Items
{
	public sealed partial class FileTagItem : ObservableObject, INavigationControlItem
	{
		public required string Text { get; set; }

		private string path = string.Empty;
		public required string Path
		{
			get => path;
			set
			{
				path = value;
				OnPropertyChanged(nameof(IconElement));
				OnPropertyChanged(nameof(ToolTip));
			}
		}

		public string ToolTipText { get; private set; } = string.Empty;

		public SectionType Section { get; set; }

		public required ContextMenuOptions MenuOptions { get; set; }

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.FileTag;

		public int CompareTo(INavigationControlItem? other)
			=> other is null ? 1 : Text.CompareTo(other.Text);

		public required TagViewModel FileTag { get; set; }

		public object? Children => null;

		public IconElement? IconElement
		{
			get
			{
				var source = new PathIconSource()
				{
					Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["App.Theme.PathIcon.FilledTag"]),
					Foreground = new SolidColorBrush(FileTag.Color.ToColor())
				};
				return source.CreateIconElement();
			}
		}

		FrameworkElement? ISidebarItemModel.IconElement => IconElement;

		public object ToolTip => Text;

		FrameworkElement? ISidebarItemModel.ItemDecorator => null;

		public bool IsExpanded { get => false; set { } }
	}
}
