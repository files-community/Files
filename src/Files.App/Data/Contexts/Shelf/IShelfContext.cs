// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contexts
{
	/// <summary>
	/// Represents context for <see cref="UserControls.ShelfPane"/>.
	/// </summary>
	public interface IShelfContext : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the currently selected items in the shelf.
		/// </summary>
		IReadOnlyList<ShelfItem> SelectedItems { get; }

		/// <summary>
		/// Gets whether any item is selected in the shelf.
		/// </summary>
		bool HasSelection { get; }
	}
}
