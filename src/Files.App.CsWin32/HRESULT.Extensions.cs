// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32
{
	public static class HRESULTExtensions
	{
		/// <summary>
		/// Throws an exception if the <see cref="HRESULT"/> indicates a failure in debug mode. Otherwise, it returns the original <see cref="HRESULT"/>.
		/// </summary>
		/// <param name="hr">Represents the result of an operation, indicating success or failure.</param>
		/// <returns>Returns the original <see cref="HRESULT"/> value regardless of the operation's success.</returns>
		public static HRESULT ThrowIfFailedOnDebug(this HRESULT hr)
		{
#if DEBUG
			if (hr.Failed)
				Marshal.ThrowExceptionForHR(hr.Value);
#endif

			return hr;
		}
	}
}
