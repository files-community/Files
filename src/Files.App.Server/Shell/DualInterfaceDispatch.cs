// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Files.App.Server;

/// <summary>
/// Implements IDispatch for a dual interface of shell32's type library
/// </summary>
/// <typeparam name="T"></typeparam>
internal static class DualInterfaceDispatch<T> where T : class
{
	private static readonly Guid ShellTypeLibraryId = new("50A7E9B0-70EF-11D1-B75A-00A0C90564FE");

	private static ITypeInfo? typeInfo;

	public static HRESULT GetTypeInfoCount(out uint count)
	{
		count = 1;
		return HRESULT.S_OK;
	}

	public static HRESULT GetTypeInfo(uint index, out ITypeInfo? result)
	{
		result = null;
		if (index != 0)
			return HRESULT.DISP_E_BADINDEX;

		result = LoadTypeInfo(out var hr);
		return hr;
	}

	public static HRESULT GetIDsOfNames(PWSTR[] names, int[] dispatchIds)
		=> LoadTypeInfo(out var hr) is { } info ? PInvoke.DispGetIDsOfNames(info, names, dispatchIds) : hr;

	public static unsafe HRESULT Invoke(T instance, int dispatchId, ushort flags, ref DISPPARAMS parameters, nint result, nint exceptionInfo, nint argumentError)
	{
		if (LoadTypeInfo(out var hr) is not { } info)
			return hr;

		// Calls the member through the vtable of the interface that the type info describes
		var pointer = ComInterfaceMarshaller<T>.ConvertToUnmanaged(instance);
		try
		{
			return PInvoke.DispInvoke(pointer, info, dispatchId, flags, ref parameters, ref Unsafe.AsRef<ComVariant>((void*)result), (EXCEPINFO*)exceptionInfo, (uint*)argumentError);
		}
		finally
		{
			ComInterfaceMarshaller<T>.Free(pointer);
		}
	}

	private static ITypeInfo? LoadTypeInfo(out HRESULT result)
	{
		result = HRESULT.S_OK;
		if (Volatile.Read(ref typeInfo) is { } cached)
			return cached;

		result = PInvoke.LoadRegTypeLib(ShellTypeLibraryId, 1, 0, 0, out var library);
		if (result.Failed)
			return null;

		result = library.GetTypeInfoOfGuid(typeof(T).GUID, out var loaded);
		return result.Failed ? null : Interlocked.CompareExchange(ref typeInfo, loaded, null) ?? loaded;
	}
}
