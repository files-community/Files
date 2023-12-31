using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;

namespace Files.App.Server;

unsafe partial class Helpers
{
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
	public static int GetActivationFactory(void* activatableClassId, void** factory)
	{
		const int E_INVALIDARG = unchecked((int)0x80070057);
		const int CLASS_E_CLASSNOTAVAILABLE = unchecked((int)0x80040111);
		const int S_OK = 0;

		if (activatableClassId is null || factory is null)
		{
			return E_INVALIDARG;
		}

		try
		{
			IntPtr obj = Module.GetActivationFactory(MarshalString.FromAbi((IntPtr)activatableClassId));

			if ((void*)obj is null)
			{
				return CLASS_E_CLASSNOTAVAILABLE;
			}

			*factory = (void*)obj;
			return S_OK;
		}
		catch (Exception e)
		{
			ExceptionHelpers.SetErrorInfo(e);
			return ExceptionHelpers.GetHRForException(e);
		}
	}
}