﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Core.Storage
{
	/// <summary>
	/// Represents a file on the file system.
	/// </summary>
	public interface IFile : IStorable
	{
		/// <summary>
		/// Opens the file and returns a <see cref="Stream"/> instance to it.
		/// </summary>
		/// <param name="access">The file access to open the file with.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is <see cref="Stream"/>, otherwise an exception is thrown.</returns>
		Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default);
	}
}
