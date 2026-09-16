// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.System.SystemServices;
using WinRT;

namespace Files.App.UserControls.TabBar
{
	/// <summary>
	/// Accepts tabs dragged from other windows onto the caption area of the title bar, which XAML drop handlers never see.
	/// </summary>
	[GeneratedComClass]
	internal sealed unsafe partial class TabDropTarget : IDropTarget
	{
		private static readonly Guid IID_IInspectable = new("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90");

		private readonly TabBar tabBar;

		private DataPackageView? draggedTab;

		public TabDropTarget(TabBar tabBar)
		{
			this.tabBar = tabBar;
		}

		public HRESULT DragEnter(IDataObject pDataObj, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
		{
			draggedTab = BaseTabBar.IsDraggingOwnTab ? null : GetDraggedTab(pDataObj);
			*pdwEffect = GetEffect();
			return HRESULT.S_OK;
		}

		public HRESULT DragOver(MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
		{
			*pdwEffect = GetEffect();
			return HRESULT.S_OK;
		}

		public HRESULT DragLeave()
		{
			draggedTab = null;
			return HRESULT.S_OK;
		}

		public HRESULT Drop(IDataObject pDataObj, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
		{
			*pdwEffect = GetEffect();
			if (draggedTab is { } tab)
				_ = tabBar.OpenDroppedTabAsync(tab);

			draggedTab = null;
			return HRESULT.S_OK;
		}

		private DROPEFFECT GetEffect()
			=> draggedTab is null ? DROPEFFECT.DROPEFFECT_NONE : DROPEFFECT.DROPEFFECT_MOVE;

		// A DataPackage dragged by another WinUI window keeps its WinRT identity behind the OLE proxy
		[DynamicWindowsRuntimeCast(typeof(DataPackage))]
		private static DataPackageView? GetDraggedTab(IDataObject dataObject)
		{
			var unknown = ComInterfaceMarshaller<IDataObject>.ConvertToUnmanaged(dataObject);
			try
			{
				if (Marshal.QueryInterface((nint)unknown, IID_IInspectable, out var inspectable) != 0)
					return null;

				var view = (MarshalInspectable<object>.FromAbi(inspectable) as DataPackage)?.GetView();
				Marshal.Release(inspectable);
				return view?.Properties.ContainsKey(BaseTabBar.TabPathIdentifier) == true ? view : null;
			}
			finally
			{
				ComInterfaceMarshaller<IDataObject>.Free(unknown);
			}
		}
	}
}
