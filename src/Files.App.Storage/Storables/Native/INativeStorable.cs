// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Storage.Storables
{
	/// <summary>
	/// Represents a file object that is natively supported by Windows Shell API.
	/// </summary>
	public class INativeStorable : NativeStorable
	{
		/// <summary>
		/// Get a property value from this <see cref="INativeStorable"/>.
		/// </summary>
		/// <param name="id">The property ID (e.g. "System.Image.Dimensions").</param>
		/// <returns>Returns a valid value formatted with string; otherwise, returns <see cref="string.Empty"/>.</returns>
        public string GetPropertyAsync(string id);
	}
}
