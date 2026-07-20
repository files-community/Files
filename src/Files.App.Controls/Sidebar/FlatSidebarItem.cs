// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace Files.App.Controls
{
	/// <summary>
	/// Per-row wrapper used by the sidebar's virtualized ItemsRepeater. Carries the row's tree depth and section-gap flags so the underlying data items don't have to.
	/// </summary>
	public sealed class FlatSidebarItem : INotifyPropertyChanged
	{
		public ISidebarItemModel Item { get; }

		public int Depth { get; }

		// Caller-supplied at construction; hidden filesystem items are dimmed to match the file list's dimming convention.
		public double RowOpacity { get; }

		private static readonly PropertyChangedEventArgs SectionGapMarginChangedArgs = new(nameof(SectionGapMargin));

		private bool _hasExpandedPredecessor;
		public bool HasExpandedPredecessor
		{
			get => _hasExpandedPredecessor;
			set
			{
				if (_hasExpandedPredecessor == value)
					return;
				_hasExpandedPredecessor = value;
				PropertyChanged?.Invoke(this, SectionGapMarginChangedArgs);
			}
		}

		public Thickness SectionGapMargin => _hasExpandedPredecessor
			? new Thickness(0, 12, 0, 0)
			: new Thickness(0);

		public event PropertyChangedEventHandler? PropertyChanged;

		public FlatSidebarItem(ISidebarItemModel item, int depth, double rowOpacity = 1.0)
		{
			Item = item;
			Depth = depth;
			RowOpacity = rowOpacity;
		}
	}
}
