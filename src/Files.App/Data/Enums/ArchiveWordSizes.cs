// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Defines constants that specify archive word size (fast bytes) for 7z LZMA/LZMA2.
	/// </summary>
	public enum ArchiveWordSizes
	{
		/// <summary>
		/// Automatic (default based on compression level).
		/// </summary>
		Auto,

		/// <summary>
		/// 8 fast bytes.
		/// </summary>
		Fb8,

		/// <summary>
		/// 16 fast bytes.
		/// </summary>
		Fb16,

		/// <summary>
		/// 32 fast bytes.
		/// </summary>
		Fb32,

		/// <summary>
		/// 64 fast bytes.
		/// </summary>
		Fb64,

		/// <summary>
		/// 128 fast bytes.
		/// </summary>
		Fb128,

		/// <summary>
		/// 256 fast bytes.
		/// </summary>
		Fb256,

		/// <summary>
		/// 273 fast bytes (maximum).
		/// </summary>
		Fb273,
	}
}
