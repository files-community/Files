// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls;

public sealed class TableViewTemplateCellEditingContext : DependencyObject
{
	public static readonly DependencyProperty ValueProperty =
		DependencyProperty.Register(
			nameof(Value),
			typeof(object),
			typeof(TableViewTemplateCellEditingContext),
			new PropertyMetadata(null));

	internal TableViewTemplateCellEditingContext(object dataItem, object? value)
	{
		DataItem = dataItem;
		Value = value;
	}

	public object DataItem { get; }

	public object? Value
	{
		get => GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}
}
