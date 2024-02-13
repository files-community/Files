// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.Data.Items
{
	public class LocationItem : ObservableObject, INavigationControlItem
	{
		public BitmapImage icon;
		public BitmapImage Icon
		{
			get => icon;
			set
			{
				SetProperty(ref icon, value, nameof(Icon));
				OnPropertyChanged(nameof(IconSource));
			}
		}

		public byte[] IconData { get; set; }

		private string text = "";
		public string Text
		{
			get => text;
			set
			{
				text = value;
				// Just in case path hasn't been set
				if (ToolTip is "")
					ToolTip = value;
			}
		}

		private string path;
		public string Path
		{
			get => path;
			set
			{
				path = value;
				ToolTip = string.IsNullOrEmpty(Path) ||
					Path.Contains('?', StringComparison.Ordinal) ||
					Path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ||
					Path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase) ||
					Path == "Home"
					? Text
					: Path;
			}
		}

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.Location;

		public bool IsDefaultLocation { get; set; }

		public object? Children => Section == SectionType.Home ? null : ChildItems;
		public BulkConcurrentObservableCollection<INavigationControlItem>? ChildItems { get; set; }
		public IconSource? IconSource
		{
			get => new ImageIconSource()
			{
				ImageSource = icon
			};
		}

		public bool SelectsOnInvoked { get; set; } = true;

		private bool isExpanded;
		public bool IsExpanded
		{
			get => isExpanded;
			set => SetProperty(ref isExpanded, value);
		}

		public bool IsInvalid { get; set; } = false;

		public bool IsPinned => App.QuickAccessManager.Model.FavoriteItems.Contains(path);

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public bool IsHeader { get; set; }

		private object toolTip = "";
		public virtual object ToolTip
		{
			get => toolTip;
			set
			{
				SetProperty(ref toolTip, value);
			}
		}

		public FrameworkElement? ItemDecorator
		{
			get
			{
				if (Section == SectionType.Favorites)
				{
					return new OpacityIcon()
					{
						Style = Application.Current.Resources["SidebarFavouritesPinnedIcon"] as Style
					};
				}
				return null;
			}
		}

		public int CompareTo(INavigationControlItem other)
			=> Text.CompareTo(other.Text);

		public static T Create<T>() where T : LocationItem, new()
		{
			return new T();
		}
	}

	public class RecycleBinLocationItem : LocationItem
	{
		public void RefreshSpaceUsed(object sender, FileSystemEventArgs e)
		{
			MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
			{
				SpaceUsed = RecycleBinHelpers.GetSize();
			});
		}

		private ulong spaceUsed;
		public ulong SpaceUsed
		{
			get => spaceUsed;
			set
			{
				if (SetProperty(ref spaceUsed, value))
					OnPropertyChanged(nameof(ToolTip));
			}
		}

		public override object ToolTip
		{
			get => SpaceUsed.ToSizeString();
		}

		public RecycleBinLocationItem()
		{
			SpaceUsed = RecycleBinHelpers.GetSize();

			RecycleBinManager.Default.RecycleBinItemCreated += RefreshSpaceUsed;
			RecycleBinManager.Default.RecycleBinItemDeleted += RefreshSpaceUsed;
		}
	}
}
