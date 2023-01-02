using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.Shared;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;

namespace Files.App.DataModels.NavigationControlItems
{
	public class LocationItem : ObservableObject, INavigationControlItem
	{
		public BitmapImage icon;

		public BitmapImage Icon
		{
			get => icon;
			set => SetProperty(ref icon, value);
		}

		//public Uri IconSource { get; set; }
		public byte[] IconData { get; set; }

		public string Text { get; set; } = "";

		private string path;

		public string Path
		{
			get => path;
			set
			{
				path = value;
				ToolTipText = string.IsNullOrEmpty(Path) || Path.Contains('?', StringComparison.Ordinal) || Path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) || Path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase) || Path == "Home".GetLocalizedResource() ? Text : Path;
            }
		}

		public virtual string ToolTipText { get; private set; }
		public FontFamily Font { get; set; }
		public NavigationControlItemType ItemType => NavigationControlItemType.Location;
		public bool IsDefaultLocation { get; set; }
		public BulkConcurrentObservableCollection<INavigationControlItem> ChildItems { get; set; }

		public bool SelectsOnInvoked { get; set; } = true;

		private bool isExpanded;
		public bool IsExpanded
		{
			get => isExpanded;
			set => SetProperty(ref isExpanded, value);
		}

		public bool IsInvalid { get; set; } = false;
		public bool IsRecycleBin => string.Equals(Path, "shell:RecycleBinFolder", StringComparison.OrdinalIgnoreCase);
		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);
	}

	public class RecycleBinLocationItem : LocationItem
	{
		public void RefreshSpaceUsed()
		{
			App.Logger.Warn("Refreshing Recycle Bin used space");
			SpaceUsed = RecycleBinHelpers.GetSize();
		}

		private ulong spaceUsed;
		public ulong SpaceUsed
		{
			get => spaceUsed;
			set { 
				if (SetProperty(ref spaceUsed, value))
					OnPropertyChanged(nameof(ToolTipText));

				App.Logger.Warn(ToolTipText + $", real value: {spaceUsed}");
			}
		}

		public override string ToolTipText => "RecycleBinSizeText".GetLocalizedResource() + SpaceUsed.ToSizeString();

		public RecycleBinLocationItem()
		{
			RecycleBinHelpers.sidebarBin = this;
		}
	}
}