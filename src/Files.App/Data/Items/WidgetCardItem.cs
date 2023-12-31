// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents base item for Widget card item.
	/// </summary>
	public abstract class WidgetCardItem : ObservableObject
	{
		/// <summary>
		/// Gets or sets a path navigates to the card item.
		/// </summary>
		public virtual string? Path { get; set; }

		/// <summary>
		/// Gets or sets item of this card.
		/// </summary>
		public virtual object? Item { get; set; }
	}
}
