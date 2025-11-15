// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;
using static Windows.Win32.ManualMacros;

namespace Files.App.Storage
{
	public static partial class WindowsStorableHelpers
	{
		public static unsafe string? GetEnvironmentVariable(string name)
		{
			using HeapPtr<char> pszBuffer = default;
			bool fRes = pszBuffer.Allocate(1024U);
			if (!fRes) return null;

			uint cchBuffer = PInvoke.GetEnvironmentVariable((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in name.GetPinnableReference())), pszBuffer.Get(), (uint)name.Length + 1);
			return cchBuffer is 0U ? null : new(pszBuffer.Get());
		}

		public static unsafe string? ResolveIndirectString(string source)
		{
			using HeapPtr<char> pszBuffer = default;
			bool fRes = pszBuffer.Allocate(1024U);
			if (!fRes) return null;

			HRESULT hr = PInvoke.SHLoadIndirectString((PCWSTR)Unsafe.AsPointer(ref Unsafe.AsRef(in source.GetPinnableReference())), pszBuffer.Get(), (uint)source.Length + 1, null);
			return FAILED(hr) ? null : new(pszBuffer.Get());
		}
	}
}
