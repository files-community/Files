using System;
using System.Collections.Generic;
using System.Text;

namespace ICSharpCode.SharpZipLib.Zip
{
	/// <summary>
	/// General ZipEntry helper extensions
	/// </summary>
	public static class ZipEntryExtensions
	{
		/// <summary>
		/// Efficiently check if a <see cref="GeneralBitFlags">flag</see> is set without enum un-/boxing
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="flag"></param>
		/// <returns>Returns whether the flag was set</returns>
		public static bool HasFlag(this ZipEntry entry, GeneralBitFlags flag)
			=> (entry.Flags & (int) flag) != 0;

		/// <summary>
		/// Efficiently set a <see cref="GeneralBitFlags">flag</see> without enum un-/boxing
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="flag"></param>
		/// <param name="enabled">Whether the passed flag should be set (1) or cleared (0)</param>
		public static void SetFlag(this ZipEntry entry, GeneralBitFlags flag, bool enabled = true)
			=> entry.Flags = enabled 
				? entry.Flags | (int) flag 
				: entry.Flags & ~(int) flag;
	}
}
