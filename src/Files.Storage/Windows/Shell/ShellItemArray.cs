// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.Storage;

public static unsafe class ShellItemArray
{
	public static IShellItemArray Create(ShellItem[] items)
	{
		var pidls = (ITEMIDLIST**)NativeMemory.AllocZeroed((nuint)items.Length, (nuint)sizeof(ITEMIDLIST*));
		try
		{
			for (int index = 0; index < items.Length; index++)
				PInvoke.SHGetIDListFromObject(items[index].IShellItem, out pidls[index]).ThrowOnFailure();
			PInvoke.SHCreateShellItemArrayFromIDLists((uint)items.Length, pidls, out var array).ThrowOnFailure();
			return array;
		}
		finally
		{
			for (int index = 0; index < items.Length; index++)
				PInvoke.CoTaskMemFree(pidls[index]);
			NativeMemory.Free(pidls);
		}
	}
}
