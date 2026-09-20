// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Windows.Win32;

public static class ComHelpers
{
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
