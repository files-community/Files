// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace Windows.Win32
{
	/// <summary>
	/// A struct that works with COM pointers safely and securely.
	/// </summary>
	public unsafe struct ComPtr<T> : IDisposable where T : unmanaged
	{
		private T* _ptr;

		public bool IsNull
			=> _ptr == default;

		public ComPtr(T* other)
		{
			_ptr = other;

			if (other is not null)
				((IUnknown*)other)->AddRef();
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T* Get()
        {
            return _ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T** GetAddress()
        {
            return (T**)Unsafe.AsPointer(ref Unsafe.AsRef(in this));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			T* pointer = _ptr;

			if (pointer is not null)
			{
				_ptr = null;
				((IUnknown*)pointer)->Release();
			}
		}
	}
}
