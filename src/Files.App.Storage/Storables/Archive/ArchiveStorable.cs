// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	/// <inheritdoc cref="IArchiveStorable"/>
	public abstract class ArchiveStorable : IArchiveStorable
	{
		/// <inheritdoc/>
		public string Path { get; protected set; }

		/// <inheritdoc/>
		public string Name { get; protected set; }

		/// <inheritdoc/>
		public string Id { get; protected set; }
	}
}
