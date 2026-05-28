// Copyright (c) Files Community
// Licensed under the MIT License.

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
		public FolderNode(string path, string name, FolderNodeKind kind, ImageSource? icon)
		{
			Path = path;
			Name = name;
			Kind = kind;
			_Icon = icon;
		}

		public string Path { get; }
		public string Name { get; }
		public FolderNodeKind Kind { get; }
		public bool IsSection => Kind == FolderNodeKind.Section;

		// The INavigationControlItem this node was built from (or a synthetic LocationItem for lazy-loaded subfolders). Carried so right-click can dispatch to SidebarViewModel.HandleItemContextInvokedAsync, which expects an INavigationControlItem.
		public INavigationControlItem? SourceItem { get; set; }

		public ObservableCollection<FolderNode> Children { get; } = new();

		private ImageSource? _Icon;
		public ImageSource? Icon
		{
			get => _Icon;
			set => SetProperty(ref _Icon, value);
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
