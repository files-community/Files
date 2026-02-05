// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace Windows.Win32
{
	/// <summary>
	/// Contains a heap pointer allocated via <see cref="PInvoke.CoTaskMemAlloc"/> and a set of methods to work with the pointer safely.
	/// </summary>
	public unsafe struct ComHeapPtr<T> : IDisposable where T : unmanaged
	{
		private T* _ptr;

		public readonly bool IsNull
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _ptr is null;
		}

		public ComHeapPtr(T* ptr)
		{
			_ptr = ptr;
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
		public void Attach(T* ptr)
		{
			Dispose();
			_ptr = ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T* Detach()
		{
			T* ptr = _ptr;
			_ptr = null;
			return ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Allocate(nuint cch)
		{
			_ptr = (T*)PInvoke.CoTaskMemAlloc(cch * (nuint)sizeof(T));
			return _ptr is not null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Reallocate(nuint cch)
		{
			T* ptr = (T*)PInvoke.CoTaskMemRealloc(_ptr, cch * (nuint)sizeof(T));
			if (ptr is null) return false;
			_ptr = ptr;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			T* ptr = _ptr;
			if (ptr is null) return;
			_ptr = null;
			PInvoke.CoTaskMemFree(ptr);
		}
	}
}
