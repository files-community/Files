﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

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
