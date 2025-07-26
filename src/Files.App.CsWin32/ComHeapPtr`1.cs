// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Windows.Win32.System.Com;

namespace Windows.Win32
{
	/// <summary>
	/// Contains a heap pointer allocated via CoTaskMemAlloc and a set of methods to work with the pointer safely.
	/// </summary>
	public unsafe struct ComHeapPtr<T> : IDisposable where T : unmanaged
	{
		private T* _ptr;

		public bool IsNull
			=> _ptr == null;

		public ComHeapPtr(T* ptr)
		{
			_ptr = ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Attach(T* other)
		{
			if (_ptr is not null)
				((IUnknown*)_ptr)->Release();

			_ptr = other;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly T* Get()
		{
			return _ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly T** GetAddressOf()
		{
			return (T**)Unsafe.AsPointer(ref Unsafe.AsRef(in this));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			T* ptr = _ptr;
			if (ptr is not null)
			{
				_ptr = null;
				PInvoke.CoTaskMemFree((void*)ptr);
			}
		}
	}
}
