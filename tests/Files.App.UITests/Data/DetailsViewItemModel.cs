// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Controls;
using System;
using System.Runtime.CompilerServices;

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

		public T GetValue<T>(string name)
		{
			switch (name)
			{
				case nameof(Name):
					var itemName = Name ?? string.Empty;
					return Unsafe.As<string, T>(ref itemName);

				case nameof(DateModified):
					var dateModified = DateModified;
					return Unsafe.As<DateTimeOffset?, T>(ref dateModified);

				case nameof(Type):
					var itemType = Type ?? string.Empty;
					return Unsafe.As<string, T>(ref itemType);

				case nameof(Size):
					var itemSize = Size ?? string.Empty;
					return Unsafe.As<string, T>(ref itemSize);

				default:
					throw new InvalidOperationException($"Unknown property '{name}'.");
			}
		}

		public TableViewCellEditResult TrySetValue<T>(string name, T value)
		{
			if (name is not nameof(Name))
				return TableViewCellEditResult.Failure();

			var newName = Unsafe.As<T, string>(ref value);
			if (string.IsNullOrWhiteSpace(newName))
				return TableViewCellEditResult.Failure("A file name is required.");

			Name = newName;
			return TableViewCellEditResult.Success;
		}
	}
}
