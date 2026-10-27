// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Represents a shell property value of an item, as displayed and as used for sorting.
	/// </summary>
	public readonly record struct ShellPropertyValue(string? Text, object? Value);

	/// <summary>
	/// Reads shell property values of items.
	/// </summary>
	public static class ShellPropertyReader
	{
		public static Task<Dictionary<string, ShellPropertyValue>> ReadAsync(string path, IReadOnlyList<string> propertyNames)
		{
			return STATask.RunPooled(() => Read(path, propertyNames), App.Logger);
		}

		private static Dictionary<string, ShellPropertyValue> Read(string path, IReadOnlyList<string> propertyNames)
		{
			var values = new Dictionary<string, ShellPropertyValue>(propertyNames.Count);
			try
			{
				using var shellItem = new ShellItem(path);
				foreach (var name in propertyNames)
					values[name] = new(shellItem.Properties.GetPropertyString(name), Normalize(shellItem.Properties[name]));
			}
			catch (Exception ex)
			{
				App.Logger.LogDebug(ex, "Failed to read shell properties for an item.");
				foreach (var name in propertyNames)
					values.TryAdd(name, default);
			}

			return values;
		}

		private static object? Normalize(object? value)
		{
			return value is System.Runtime.InteropServices.ComTypes.FILETIME fileTime
				? DateTimeOffset.FromFileTime(((long)fileTime.dwHighDateTime << 32) | (uint)fileTime.dwLowDateTime)
				: value;
		}
	}
}
