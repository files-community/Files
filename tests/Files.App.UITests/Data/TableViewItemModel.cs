// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Controls;
using System;

namespace Files.App.UITests.Data
{
	internal partial class TableViewItemModel : ObservableObject, ITableViewCellValueProvider, ITableViewCellValueEditor
	{
		[ObservableProperty]
		public partial string? Name { get; set; }

		[ObservableProperty]
		public partial DateTimeOffset? DateUpdated { get; set; }

		[ObservableProperty]
		public partial string? Type { get; set; }

		[ObservableProperty]
		public partial string? Size { get; set; }

		public object? GetValue(string name)
		{
			return name switch
			{
				nameof(Name) => Name ?? string.Empty,
				nameof(DateUpdated) => DateUpdated,
				nameof(Type) => Type ?? string.Empty,
				nameof(Size) => Size ?? string.Empty,
				_ => throw new InvalidOperationException($"Unknown property '{name}'."),
			};
		}

		public TableViewCellEditResult TrySetValue(string name, object? value)
		{
			switch (name)
			{
				case nameof(Name):
					Name = (string?)value;
					return TableViewCellEditResult.Success;
				case nameof(DateUpdated):
					DateUpdated = (DateTimeOffset?)value;
					return TableViewCellEditResult.Success;
				case nameof(Type):
					Type = (string?)value;
					return TableViewCellEditResult.Success;
				case nameof(Size):
					Size = (string?)value;
					return TableViewCellEditResult.Success;
				default:
					return TableViewCellEditResult.Failure($"Unknown property '{name}'.");
			}
		}
	}
}
