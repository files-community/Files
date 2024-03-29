//------------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by cswinrt.exe version 2.0.3.230608.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT;
using WinRT.Interop;


#pragma warning disable 0169 // warning CS0169: The field '...' is never used
#pragma warning disable 0649 // warning CS0169: Field '...' is never assigned to
#pragma warning disable CA2207, CA1063, CA1033, CA1001, CA2213

namespace Microsoft.UI.Content
{
	[global::WinRT.WindowsRuntimeType("Microsoft.UI.Content")][global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Content.ContentExternalOutputLink))]
	[global::WinRT.ProjectedRuntimeClass(typeof(IContentExternalOutputLink))]
	[global::WinRT.ObjectReferenceWrapper(nameof(_inner))]
	[global::Windows.Foundation.Metadata.ContractVersion(typeof(global::Microsoft.Foundation.WindowsAppSDKContract), 65540u)]
	[global::Windows.Foundation.Metadata.Experimental]
	public sealed class ContentExternalOutputLink : global::System.IDisposable, global::System.Runtime.InteropServices.ICustomQueryInterface, IWinRTObject, IEquatable<ContentExternalOutputLink>
	{
		private IntPtr ThisPtr => _inner == null ? (((IWinRTObject)this).NativeObject).ThisPtr : _inner.ThisPtr;

		private IObjectReference _inner = null;

		private volatile global::System.IDisposable _lazy_global__System_IDisposable;
		private global::System.IDisposable Make__lazy_global__System_IDisposable()
		{
			global::System.Threading.Interlocked.CompareExchange(ref _lazy_global__System_IDisposable, (global::System.IDisposable)(object)new SingleInterfaceOptimizedObject(typeof(global::System.IDisposable), _inner ?? ((IWinRTObject)this).NativeObject), null);
			return _lazy_global__System_IDisposable;
		}



		private IObjectReference _objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLink => _inner;
		private IContentExternalOutputLink _default => null;

		public static I As<I>() => ActivationFactory<ContentExternalOutputLink>.AsInterface<I>();

		private static volatile IObjectReference ___objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLinkStatics;
		private static IObjectReference Make___objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLinkStatics()
		{
			global::System.Threading.Interlocked.CompareExchange(ref ___objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLinkStatics, ActivationFactory<ContentExternalOutputLink>.As(GuidGenerator.GetIID(typeof(IContentExternalOutputLinkStatics).GetHelperType())), null);
			return ___objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLinkStatics;
		}
		private static IObjectReference _objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLinkStatics => ___objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLinkStatics ?? Make___objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLinkStatics();



		public static ContentExternalOutputLink Create(global::Microsoft.UI.Composition.Compositor compositor) => global::ABI.Microsoft.UI.Content.IContentExternalOutputLinkStaticsMethods.Create(_objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLinkStatics, compositor);

		public static bool IsSupported() => global::ABI.Microsoft.UI.Content.IContentExternalOutputLinkStaticsMethods.IsSupported(_objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLinkStatics);

		public static ContentExternalOutputLink FromAbi(IntPtr thisPtr)
		{
			if (thisPtr == IntPtr.Zero) return null;
			return MarshalInspectable<ContentExternalOutputLink>.FromAbi(thisPtr);
		}

		internal ContentExternalOutputLink(IObjectReference objRef)
		{
			_inner = objRef.As(GuidGenerator.GetIID(typeof(IContentExternalOutputLink).GetHelperType()));

		}

		public static bool operator ==(ContentExternalOutputLink x, ContentExternalOutputLink y) => (x?.ThisPtr ?? IntPtr.Zero) == (y?.ThisPtr ?? IntPtr.Zero);
		public static bool operator !=(ContentExternalOutputLink x, ContentExternalOutputLink y) => !(x == y);
		public bool Equals(ContentExternalOutputLink other) => this == other;
		public override bool Equals(object obj) => obj is ContentExternalOutputLink that && this == that;
		public override int GetHashCode() => ThisPtr.GetHashCode();


		bool IWinRTObject.HasUnwrappableNativeObject => true;
		IObjectReference IWinRTObject.NativeObject => _inner;
		private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> _queryInterfaceCache;
		private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> MakeQueryInterfaceCache()
		{
			global::System.Threading.Interlocked.CompareExchange(ref _queryInterfaceCache, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference>(), null); 
			return _queryInterfaceCache;
		}
		global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, IObjectReference> IWinRTObject.QueryInterfaceCache => _queryInterfaceCache ?? MakeQueryInterfaceCache();
		private volatile global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> _additionalTypeData;
		private global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> MakeAdditionalTypeData()
		{
			global::System.Threading.Interlocked.CompareExchange(ref _additionalTypeData, new global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object>(), null); 
			return _additionalTypeData;
		}
		global::System.Collections.Concurrent.ConcurrentDictionary<RuntimeTypeHandle, object> IWinRTObject.AdditionalTypeData => _additionalTypeData ?? MakeAdditionalTypeData();

		private struct InterfaceTag<I>{};


		private global::System.IDisposable AsInternal(InterfaceTag<global::System.IDisposable> _) => _lazy_global__System_IDisposable ?? Make__lazy_global__System_IDisposable();

		public void Dispose() => AsInternal(new InterfaceTag<global::System.IDisposable>()).Dispose();

		public global::Windows.UI.Color BackgroundColor
		{
			get => global::ABI.Microsoft.UI.Content.IContentExternalOutputLinkMethods.get_BackgroundColor(_objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLink);
			set => global::ABI.Microsoft.UI.Content.IContentExternalOutputLinkMethods.set_BackgroundColor(_objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLink, value);
		}

		public global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue => global::ABI.Microsoft.UI.Content.IContentExternalOutputLinkMethods.get_DispatcherQueue(_objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLink);

		public global::Microsoft.UI.Composition.CompositionBorderMode ExternalOutputBorderMode
		{
			get => global::ABI.Microsoft.UI.Content.IContentExternalOutputLinkMethods.get_ExternalOutputBorderMode(_objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLink);
			set => global::ABI.Microsoft.UI.Content.IContentExternalOutputLinkMethods.set_ExternalOutputBorderMode(_objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLink, value);
		}

		public global::Microsoft.UI.Composition.Visual PlacementVisual => global::ABI.Microsoft.UI.Content.IContentExternalOutputLinkMethods.get_PlacementVisual(_objRef_global__Microsoft_UI_Content_Private_IContentExternalOutputLink);

		private bool IsOverridableInterface(Guid iid) => false;

		global::System.Runtime.InteropServices.CustomQueryInterfaceResult global::System.Runtime.InteropServices.ICustomQueryInterface.GetInterface(ref Guid iid, out IntPtr ppv)
		{
			ppv = IntPtr.Zero;
			if (IsOverridableInterface(iid) || global::WinRT.InterfaceIIDs.IInspectable_IID == iid)
			{
				return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
			}

			if (((IWinRTObject)this).NativeObject.TryAs(iid, out ppv) >= 0)
			{
				return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.Handled;
			}

			return global::System.Runtime.InteropServices.CustomQueryInterfaceResult.NotHandled;
		}
	}

	[global::WinRT.WindowsRuntimeType("Microsoft.UI.Content")]
	[Guid("FED9A1E8-F804-5A26-A8B0-ED077215D422")]
	[global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Content.IContentExternalOutputLink))]
	[global::Windows.Foundation.Metadata.ContractVersion(typeof(global::Microsoft.Foundation.WindowsAppSDKContract), 65540u)]
	[global::Windows.Foundation.Metadata.Experimental]
	internal interface IContentExternalOutputLink
	{
		global::Windows.UI.Color BackgroundColor { get; set; }
		global::Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; }
		global::Microsoft.UI.Composition.CompositionBorderMode ExternalOutputBorderMode { get; set; }
		global::Microsoft.UI.Composition.Visual PlacementVisual { get; }
	}

	[global::WinRT.WindowsRuntimeType("Microsoft.UI.Content")]
	[Guid("B758F401-833E-587D-B0CD-A3934EBA3721")]
	[global::WinRT.WindowsRuntimeHelperType(typeof(global::ABI.Microsoft.UI.Content.IContentExternalOutputLinkStatics))]
	[global::Windows.Foundation.Metadata.ContractVersion(typeof(global::Microsoft.Foundation.WindowsAppSDKContract), 65540u)]
	[global::Windows.Foundation.Metadata.Experimental]
	internal interface IContentExternalOutputLinkStatics
	{
		ContentExternalOutputLink Create(global::Microsoft.UI.Composition.Compositor compositor);
		bool IsSupported();
	}
}

#pragma warning disable CA1416
namespace ABI.Microsoft.UI.Content
{
	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
	public struct ContentExternalOutputLink
	{

		public static IObjectReference CreateMarshaler(global::Microsoft.UI.Content.ContentExternalOutputLink obj) => obj is null ? null : MarshalInspectable<global::Microsoft.UI.Content.ContentExternalOutputLink>.CreateMarshaler<IUnknownVftbl>(obj, GuidGenerator.GetIID(typeof(global::Microsoft.UI.Content.IContentExternalOutputLink).GetHelperType()));
		public static ObjectReferenceValue CreateMarshaler2(global::Microsoft.UI.Content.ContentExternalOutputLink obj) => MarshalInspectable<object>.CreateMarshaler2(obj, GuidGenerator.GetIID(typeof(global::Microsoft.UI.Content.IContentExternalOutputLink).GetHelperType()));
		public static IntPtr GetAbi(IObjectReference value) => value is null ? IntPtr.Zero : MarshalInterfaceHelper<object>.GetAbi(value);
		public static global::Microsoft.UI.Content.ContentExternalOutputLink FromAbi(IntPtr thisPtr) => global::Microsoft.UI.Content.ContentExternalOutputLink.FromAbi(thisPtr);
		public static IntPtr FromManaged(global::Microsoft.UI.Content.ContentExternalOutputLink obj) => obj is null ? IntPtr.Zero : CreateMarshaler2(obj).Detach();
		public static unsafe MarshalInterfaceHelper<global::Microsoft.UI.Content.ContentExternalOutputLink>.MarshalerArray CreateMarshalerArray(global::Microsoft.UI.Content.ContentExternalOutputLink[] array) => MarshalInterfaceHelper<global::Microsoft.UI.Content.ContentExternalOutputLink>.CreateMarshalerArray2(array, (o) => CreateMarshaler2(o));
		public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<global::Microsoft.UI.Content.ContentExternalOutputLink>.GetAbiArray(box);
		public static unsafe global::Microsoft.UI.Content.ContentExternalOutputLink[] FromAbiArray(object box) => MarshalInterfaceHelper<global::Microsoft.UI.Content.ContentExternalOutputLink>.FromAbiArray(box, FromAbi);
		public static (int length, IntPtr data) FromManagedArray(global::Microsoft.UI.Content.ContentExternalOutputLink[] array) => MarshalInterfaceHelper<global::Microsoft.UI.Content.ContentExternalOutputLink>.FromManagedArray(array, (o) => FromManaged(o));
		public static void DisposeMarshaler(IObjectReference value) => MarshalInspectable<object>.DisposeMarshaler(value);
		public static void DisposeMarshalerArray(MarshalInterfaceHelper<global::Microsoft.UI.Content.ContentExternalOutputLink>.MarshalerArray array) => MarshalInterfaceHelper<global::Microsoft.UI.Content.ContentExternalOutputLink>.DisposeMarshalerArray(array);
		public static void DisposeAbi(IntPtr abi) => MarshalInspectable<object>.DisposeAbi(abi);
		public static unsafe void DisposeAbiArray(object box) => MarshalInspectable<object>.DisposeAbiArray(box);
	}

	internal static class IContentExternalOutputLinkMethods
	{
		public static unsafe global::Windows.UI.Color get_BackgroundColor(IObjectReference _obj)
		{
			var ThisPtr = _obj.ThisPtr;

			global::Windows.UI.Color __retval = default;
			global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, out global::Windows.UI.Color, int>**)ThisPtr)[6](ThisPtr, out __retval));
			return __retval;
		}
		public static unsafe void set_BackgroundColor(IObjectReference _obj, global::Windows.UI.Color value)
		{
			var ThisPtr = _obj.ThisPtr;

			global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, global::Windows.UI.Color, int>**)ThisPtr)[7](ThisPtr, value));
		}

		public static unsafe global::Microsoft.UI.Dispatching.DispatcherQueue get_DispatcherQueue(IObjectReference _obj)
		{
			var ThisPtr = _obj.ThisPtr;

			IntPtr __retval = default;
			try
			{
				global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>**)ThisPtr)[8](ThisPtr, out __retval));
				return global::ABI.Microsoft.UI.Dispatching.DispatcherQueue.FromAbi(__retval);
			}
			finally
			{
				global::ABI.Microsoft.UI.Dispatching.DispatcherQueue.DisposeAbi(__retval);
			}
		}

		public static unsafe global::Microsoft.UI.Composition.CompositionBorderMode get_ExternalOutputBorderMode(IObjectReference _obj)
		{
			var ThisPtr = _obj.ThisPtr;

			global::Microsoft.UI.Composition.CompositionBorderMode __retval = default;
			global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, out global::Microsoft.UI.Composition.CompositionBorderMode, int>**)ThisPtr)[9](ThisPtr, out __retval));
			return __retval;
		}
		public static unsafe void set_ExternalOutputBorderMode(IObjectReference _obj, global::Microsoft.UI.Composition.CompositionBorderMode value)
		{
			var ThisPtr = _obj.ThisPtr;

			global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, global::Microsoft.UI.Composition.CompositionBorderMode, int>**)ThisPtr)[10](ThisPtr, value));
		}

		public static unsafe global::Microsoft.UI.Composition.Visual get_PlacementVisual(IObjectReference _obj)
		{
			var ThisPtr = _obj.ThisPtr;

			IntPtr __retval = default;
			try
			{
				global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, out IntPtr, int>**)ThisPtr)[11](ThisPtr, out __retval));
				return global::ABI.Microsoft.UI.Composition.Visual.FromAbi(__retval);
			}
			finally
			{
				global::ABI.Microsoft.UI.Composition.Visual.DisposeAbi(__retval);
			}
		}
	}

	[Guid("FED9A1E8-F804-5A26-A8B0-ED077215D422")]
	internal interface IContentExternalOutputLink : global::Microsoft.UI.Content.IContentExternalOutputLink
	{
	}

	internal static class IContentExternalOutputLinkStaticsMethods
	{

		public static unsafe global::Microsoft.UI.Content.ContentExternalOutputLink Create(IObjectReference _obj, global::Microsoft.UI.Composition.Compositor compositor)
		{
			var ThisPtr = _obj.ThisPtr;

			ObjectReferenceValue __compositor = default;
			IntPtr __retval = default;
			try
			{
				__compositor = global::ABI.Microsoft.UI.Composition.Compositor.CreateMarshaler2(compositor);
				global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, IntPtr, out IntPtr, int>**)ThisPtr)[6](ThisPtr, MarshalInspectable<object>.GetAbi(__compositor), out __retval));
				return global::ABI.Microsoft.UI.Content.ContentExternalOutputLink.FromAbi(__retval);
			}
			finally
			{
				MarshalInspectable<object>.DisposeMarshaler(__compositor);
				global::ABI.Microsoft.UI.Content.ContentExternalOutputLink.DisposeAbi(__retval);
			}
		}

		public static unsafe bool IsSupported(IObjectReference _obj)
		{
			var ThisPtr = _obj.ThisPtr;

			byte __retval = default;
			global::WinRT.ExceptionHelpers.ThrowExceptionForHR((*(delegate* unmanaged[Stdcall]<IntPtr, out byte, int>**)ThisPtr)[7](ThisPtr, out __retval));
			return __retval != 0;
		}

	}

	[Guid("B758F401-833E-587D-B0CD-A3934EBA3721")]
	internal interface IContentExternalOutputLinkStatics : global::Microsoft.UI.Content.IContentExternalOutputLinkStatics
	{
	}
}
#pragma warning restore CA1416
