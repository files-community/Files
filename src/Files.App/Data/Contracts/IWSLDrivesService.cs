// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides service to manage Windows Subsystem for Linux drives.
	/// </summary>
	internal interface IWSLDrivesService
	{
		/// <summary>
		/// Gets invoked when the WSL drive collection.
		/// </summary>
		event EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		/// <summary>
		/// Gets all WSL drives.
		/// </summary>
		IReadOnlyList<WslDistroItem> WSLDrives { get; }

		/// <summary>
		/// Updates all WSL drives to get the latest information.
		/// </summary>
		/// <returns></returns>
		Task UpdateDrivesAsync();

		/// <summary>
		/// Tries get a WSL drive item specified by the path.
		/// </summary>
		/// <param name="path">The path of the WSL.</param>
		/// <param name="item">The WSL item to be retreived.</param>
		/// <returns></returns>
		bool TryGet(string path, [NotNullWhen(true)] out WslDistroItem? item);
	}
}
