// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls;

public readonly record struct TableViewCellEditResult(bool Succeeded, object? ErrorContent)
{
	public static TableViewCellEditResult Success { get; } = new(true, null);

	public static TableViewCellEditResult Failure(object? errorContent = null) => new(false, errorContent);
}
