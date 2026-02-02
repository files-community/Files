// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Windows.Win32
{
	/// <summary>
	/// Contains a heap pointer allocated via <see cref="NativeMemory.Alloc"/> and a set of methods to work with the pointer safely.
	/// </summary>
	public unsafe struct HeapPtr<T> : IDisposable where T : unmanaged
	{
		private T* _ptr;

		public readonly bool IsNull
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _ptr is null;
		}

		public HeapPtr(T* ptr)
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
			_ptr = (T*)NativeMemory.Alloc(cch); // malloc()
			return _ptr is not null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Reallocate(nuint cch)
		{
			T* ptr = (T*)NativeMemory.Realloc(_ptr, cch); // realloc()
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
			NativeMemory.Free(ptr); // free()
		}
	}
}
