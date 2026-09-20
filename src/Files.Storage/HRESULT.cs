// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Windows.Win32.Foundation
{
	[DebuggerDisplay("{" + nameof(Value) + ",h}")]
	public readonly partial struct HRESULT
	{
		/// <summary>
		/// Throws an exception if the <see cref="HRESULT"/> indicates a failure in debug mode. Otherwise, it returns the original <see cref="HRESULT"/>.
		/// </summary>
		/// <returns>Returns the original <see cref="HRESULT"/> value regardless of the operation's success.</returns>
		public readonly HRESULT ThrowIfFailedOnDebug()
		{
#if DEBUG
			if (Failed) Marshal.ThrowExceptionForHR(Value);
#endif

			return this;
		}
	}
}
