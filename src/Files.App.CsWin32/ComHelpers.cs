// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Windows.Win32;

public static partial class ComHelpers
{
	public static unsafe bool CanCreateInstance(Guid classId, CLSCTX context, Guid interfaceId)
	{
		nint instance = 0;
		try
		{
			return CoCreateInstance(&classId, 0, context, &interfaceId, &instance).Succeeded;
		}
		finally
		{
			if (instance != 0)
				Marshal.Release(instance);
		}
	}

	// The generated overload creates a shared RCW; probing needs deterministic ownership instead.
	[LibraryImport("OLE32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static unsafe partial HRESULT CoCreateInstance(Guid* classId, nint outer, CLSCTX context, Guid* interfaceId, nint* instance);

	public static HRESULT TryCast<TInterface>(object nativeObject, out TInterface? instance)
		where TInterface : class
	{
		instance = null;

		if (nativeObject is not TInterface casted)
			return HRESULT.E_NOINTERFACE;

		instance = casted;
		return HRESULT.S_OK;
	}
}
