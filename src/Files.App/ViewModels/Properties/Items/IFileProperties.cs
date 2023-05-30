﻿// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.Properties
{
	internal interface IFileProperties
	{
		/// <summary>
		/// Loads metadata stored in files.
		/// </summary>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task GetSystemFilePropertiesAsync();

		/// <summary>
		/// Saves edited metadata to files.
		/// </summary>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task SyncPropertyChangesAsync();

		/// <summary>
		/// Clears metadata stored in files.
		/// </summary>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task ClearPropertiesAsync();
	}
}
