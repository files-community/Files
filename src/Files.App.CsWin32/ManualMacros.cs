// Copyright (c) Files Community
// Licensed under the MIT License.

global using static global::Windows.Win32.ManualMacros;

using Windows.Win32.Foundation;

namespace Windows.Win32
{
	public class ManualMacros
	{
		public static bool SUCCEEDED(HRESULT hr)
		{
			return hr >= 0;
		}

		public static bool FAILED(HRESULT hr)
		{
			return hr < 0;
		}
	}
}
