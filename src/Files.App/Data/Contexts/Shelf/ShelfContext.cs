// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contexts
{
	/// <inheritdoc cref="IShelfContext"/>
	internal sealed partial class ShelfContext : ObservableObject, IShelfContext
	{
		private static readonly IReadOnlyList<ShelfItem> emptySelection = [];

		private IReadOnlyList<ShelfItem> _SelectedItems = emptySelection;
		public IReadOnlyList<ShelfItem> SelectedItems => _SelectedItems;

		public bool HasSelection => _SelectedItems.Count > 0;

		public ShelfContext()
		{
			ShelfViewModel.SelectedItemsChanged += ShelfViewModel_SelectedItemsChanged;
		}

		private void ShelfViewModel_SelectedItemsChanged(object? sender, IReadOnlyList<ShelfItem> e)
		{
			if (SetProperty(ref _SelectedItems, e ?? emptySelection, nameof(SelectedItems)))
				OnPropertyChanged(nameof(HasSelection));
		}
	}
}
