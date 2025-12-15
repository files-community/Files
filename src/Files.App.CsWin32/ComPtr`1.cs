// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Windows.Win32
{
	/// <summary>
	/// Contains a COM pointer and a set of methods to work with the pointer safely.
	/// </summary>
	public unsafe struct ComPtr<T> : IDisposable where T : unmanaged, IComIID
	{
		private T* _ptr;

		public readonly bool IsNull
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _ptr is null;
		}

		// Constructors

		public ComPtr(T* ptr)
		{
			_ptr = ptr;

			if (ptr is not null)
				((IUnknown*)ptr)->AddRef();
		}

		// Methods

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Attach(T* other)
		{
			if (_ptr is not null)
				((IUnknown*)_ptr)->Release();

			_ptr = other;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T* Detach()
		{
			T* ptr = _ptr;
			_ptr = null;
			return ptr;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly HRESULT CopyTo(T** ptr)
		{
			InternalAddRef();
			*ptr = _ptr;

			return HRESULT.S_OK;
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
		public readonly HRESULT As<U>(U** other) where U : unmanaged, IComIID
		{
			return ((IUnknown*)_ptr)->QueryInterface((Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in U.Guid)), (void**)other);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly HRESULT As<U>(Guid* riid, IUnknown** other) where U : unmanaged, IComIID
		{
			return ((IUnknown*)_ptr)->QueryInterface(riid, (void**)other);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly HRESULT CoCreateInstance(Guid* rclsid, IUnknown* pUnkOuter = null, CLSCTX dwClsContext = CLSCTX.CLSCTX_LOCAL_SERVER)
		{
			return PInvoke.CoCreateInstance(rclsid, pUnkOuter, dwClsContext, (Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in T.Guid)), (void**)this.GetAddressOf());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private readonly void InternalAddRef()
		{
			T* ptr = _ptr;
			if (ptr != null)
				_ = ((IUnknown*)ptr)->AddRef();
		}

		// Disposer

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Dispose()
		{
			T* ptr = _ptr;
			if (ptr is not null)
			{
				_ptr = null;
				((IUnknown*)ptr)->Release();
			}
		}
	}
}
