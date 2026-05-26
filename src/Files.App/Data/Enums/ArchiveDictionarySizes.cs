// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Defines constants that specify archive dictionary size for 7z LZMA/LZMA2.
	/// </summary>
	public enum ArchiveDictionarySizes
	{
		/// <summary>
		/// Automatic (default based on compression level).
		/// </summary>
		Auto,

		/// <summary>
		/// 64 KB dictionary.
		/// </summary>
		Kb64,

		/// <summary>
		/// 256 KB dictionary.
		/// </summary>
		Kb256,

		/// <summary>
		/// 1 MB dictionary.
		/// </summary>
		Mb1,

		/// <summary>
		/// 2 MB dictionary.
		/// </summary>
		Mb2,

		/// <summary>
		/// 4 MB dictionary.
		/// </summary>
		Mb4,

		/// <summary>
		/// 8 MB dictionary.
		/// </summary>
		Mb8,

		/// <summary>
		/// 16 MB dictionary.
		/// </summary>
		Mb16,

		/// <summary>
		/// 32 MB dictionary.
		/// </summary>
		Mb32,

		/// <summary>
		/// 64 MB dictionary.
		/// </summary>
		Mb64,

		/// <summary>
		/// 128 MB dictionary.
		/// </summary>
		Mb128,

		/// <summary>
		/// 256 MB dictionary.
		/// </summary>
		Mb256,

		/// <summary>
		/// 512 MB dictionary.
		/// </summary>
		Mb512,

		/// <summary>
		/// 1024 MB dictionary.
		/// </summary>
		Mb1024,
	}
}
