﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage
{
	/// <summary>
	/// Provides direct copy operation of storage objects.
	/// </summary>
	public interface IDirectCopy : IModifiableStorable
	{
		/// <summary>
		/// Creates a copy of the provided storable item in this folder.
		/// </summary>
		Task<INestedStorable> CopyAsync(INestedStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default);
	}
}
