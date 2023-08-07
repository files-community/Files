// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Files.App.Converters;
using Files.Core.ViewModels.FileTags;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Items
{
	public class FileTagItem : ObservableObject, INavigationControlItem
	{
		public string Text { get; set; }

		private string path;
		public string Path
		{
			get => path;
			set
			{
				path = value;
				Debug.WriteLine($"[{System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")}] ** ** ** Loaded icon for {Path}");
				OnPropertyChanged("Icon");
				OnPropertyChanged(nameof(ToolTip));
			}
		}

		public string ToolTipText { get; private set; }

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.FileTag;

		public int CompareTo(INavigationControlItem other)
			=> Text.CompareTo(other.Text);

		public TagViewModel FileTag { get; set; }

		public BulkConcurrentObservableCollection<INavigationControlItem>? ChildItems => null;

		public IconSource? GenerateIconSource() => new PathIconSource()
		{
			Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["ColorIconFilledTag"]),
			Foreground = new SolidColorBrush(FileTag.Color.ToColor())
		};
		
		public object ToolTip => Text;

		public bool IsExpanded { get => false; set { } }
	}
}
