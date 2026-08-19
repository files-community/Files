// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using WinRT;

namespace Files.App.Data.Items
{
	public sealed partial class FileTagItem : ObservableObject, INavigationControlItem
	{
		public string? Text { get; set; }

		private string? path;
		public string? Path
		{
			get => path;
			set
			{
				path = value;
				OnPropertyChanged(nameof(IconElement));
				OnPropertyChanged(nameof(ToolTip));
			}
		}

		public string? ToolTipText { get; private set; }

		public SectionType Section { get; set; }

		public ContextMenuOptions? MenuOptions { get; set; }

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.FileTag;

		public int CompareTo(INavigationControlItem? other)
		{
			var text = Text ?? throw new InvalidOperationException("The file tag name has not been initialized.");
			var otherText = other?.Text
				?? throw new ArgumentException("The compared item must have a name.", nameof(other));

			return text.CompareTo(otherText);
		}

		public TagViewModel? FileTag { get; set; }

		public object? Children => null;

		public IconElement? IconElement
		{
			[DynamicWindowsRuntimeCast(typeof(Geometry))]
			get
			{
				var source = new PathIconSource()
				{
					Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["App.Theme.PathIcon.FilledTag"]),
					Foreground = new SolidColorBrush((FileTag
						?? throw new InvalidOperationException("The file tag has not been initialized.")).Color.ToColor())
				};
				return source.CreateIconElement();
			}
		}

		FrameworkElement? ISidebarItemPresentationModel.IconElement => IconElement;

		public object? ToolTip => Text;

		FrameworkElement? ISidebarItemPresentationModel.ItemDecorator => null;

		public bool IsExpanded { get => false; set { } }
	}
}
