// Copyright(c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.Storage
{
	public interface IStorableProperties
	{
		ulong Size { get; }

		DateTimeOffset DateCreated { get; }

		DateTimeOffset DateModified { get; }
	}
}
