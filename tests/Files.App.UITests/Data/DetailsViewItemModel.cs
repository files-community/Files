// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Controls;
using System;

namespace Files.App.UITests.Data
{
	internal partial class DetailsViewItemModel : ObservableObject, ITableViewCellValueProvider, ITableViewCellValueEditor
	{
		[ObservableProperty]
		public partial string? Name { get; set; }

		[ObservableProperty]
		public partial DateTimeOffset? DateModified { get; set; }

		[ObservableProperty]
		public partial string? Type { get; set; }

		[ObservableProperty]
		public partial string? Size { get; set; }

		[ObservableProperty]
		public partial string? IconGlyph { get; set; }

		public object? GetValue(string name)
		{
			return name switch
			{
				nameof(Name) => Name ?? string.Empty,
				nameof(DateModified) => DateModified,
				nameof(Type) => Type ?? string.Empty,
				nameof(Size) => Size ?? string.Empty,
				_ => throw new InvalidOperationException($"Unknown property '{name}'."),
			};
		}

		public TableViewCellEditResult TrySetValue(string name, object? value)
		{
			if (name is not nameof(Name))
				return TableViewCellEditResult.Failure();

			var newName = value as string;
			if (string.IsNullOrWhiteSpace(newName))
				return TableViewCellEditResult.Failure("A file name is required.");

			Name = newName;
			return TableViewCellEditResult.Success;
		}
	}
}
