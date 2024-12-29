// Copyright (c) 2018-2025 Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents base item for widget card item.
	/// </summary>
	public abstract class WidgetCardItem : ObservableObject
	{
		public virtual string? Path { get; set; }

		public virtual object? Item { get; set; }
	}
}
