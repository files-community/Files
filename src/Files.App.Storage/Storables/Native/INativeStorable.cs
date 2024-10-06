// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Storage.Storables
{
	/// <summary>
	/// Represents a storable that is natively supported by Windows Shell API.
	/// </summary>
	public interface INativeStorable : IStorable
	{
       public string GetPropertyAsync(string id);
	}
}
