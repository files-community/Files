// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Controls;
using System;
using System.Runtime.CompilerServices;

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

		public T GetValue<T>(string name)
		{
			switch (name)
			{
				case nameof(Name):
					{
						var currentValue = Name ?? string.Empty;
						return Unsafe.As<string, T>(ref currentValue);
					}
				case nameof(DateUpdated):
					{
						var currentValue = DateUpdated;
						return Unsafe.As<DateTimeOffset?, T>(ref currentValue);
					}
				case nameof(Type):
					{
						var currentValue = Type ?? string.Empty;
						return Unsafe.As<string, T>(ref currentValue);
					}
				case nameof(Size):
					{
						var currentValue = Size ?? string.Empty;
						return Unsafe.As<string, T>(ref currentValue);
					}
				default:
					throw new InvalidOperationException($"Unknown property '{name}'.");
			}
		}

		public bool TrySetValue<T>(string name, T value)
		{
			switch (name)
			{
				case nameof(Name):
					Name = Unsafe.As<T, string>(ref value);
					return true;
				case nameof(DateUpdated):
					DateUpdated = Unsafe.As<T, DateTimeOffset?>(ref value);
					return true;
				case nameof(Type):
					Type = Unsafe.As<T, string>(ref value);
					return true;
				case nameof(Size):
					Size = Unsafe.As<T, string>(ref value);
					return true;
				default:
					return false;
			}
		}
	}
}
