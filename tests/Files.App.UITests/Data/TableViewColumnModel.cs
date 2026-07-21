// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.UITests.Data;

internal enum TableViewColumnValueType
{
	Text,
	DateTimeOffset,
}

internal class TableViewColumnModel(string header, string propertyName, TableViewColumnValueType valueType = TableViewColumnValueType.Text)
{
	public string? Header { get; set; } = header;

	public string? PropertyName { get; set; } = propertyName;

	public TableViewColumnValueType ValueType { get; set; } = valueType;
}
