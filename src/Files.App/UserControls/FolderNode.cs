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
	}
}
