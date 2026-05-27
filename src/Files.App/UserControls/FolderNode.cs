// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.UserControls
{
	public enum FolderNodeKind
	{
		Section,
		Folder,
		Leaf,
	}

	public sealed partial class FolderNode : ObservableObject
	{
		public FolderNode(string path, string name, FolderNodeKind kind, ImageSource? icon, INavigationControlItem? sourceItem = null)
		{
			Path = path;
			Name = name;
			Kind = kind;
			_Icon = icon;
			SourceItem = sourceItem;
		}

		public string Path { get; }
		public string Name { get; }
		public FolderNodeKind Kind { get; }
		public bool IsSection => Kind == FolderNodeKind.Section;
		public INavigationControlItem? SourceItem { get; }

		public ObservableCollection<FolderNode> Children { get; } = new();

		// ImageSource (not IconElement/FrameworkElement) — UIElements can only have one visual parent; storing one in a data model crashes WinUI when a TreeViewItem container is recycled.
		private ImageSource? _Icon;
		public ImageSource? Icon
		{
			get => _Icon;
			set => SetProperty(ref _Icon, value);
		}

		private IconSource? _TagIconSource;
		public IconSource? TagIconSource
		{
			get => _TagIconSource;
			set => SetProperty(ref _TagIconSource, value);
		}

		private bool _IsExpanded;
		public bool IsExpanded
		{
			get => _IsExpanded;
			set => SetProperty(ref _IsExpanded, value);
		}

		private bool _HasUnrealizedChildren;
		public bool HasUnrealizedChildren
		{
			get => _HasUnrealizedChildren;
			set => SetProperty(ref _HasUnrealizedChildren, value);
		}

		private double _Opacity = 1.0;
		public double Opacity
		{
			get => _Opacity;
			set => SetProperty(ref _Opacity, value);
		}

		// Custom selection flag driving the DataTemplate's overlay Border. Set by the path-mirror code in TreeViewSidebar — we deliberately do NOT go through TreeView.SelectedItem, since assigning that to a data node whose TreeViewItem container isn't realized crashes WinUI's native selection machinery (ExecutionEngineException).
		private bool _IsSelected;
		public bool IsSelected
		{
			get => _IsSelected;
			set => SetProperty(ref _IsSelected, value);
		}
	}
}
