// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Utils.Shell
{
	/// <summary>Provides the AOT-safe vtable calls needed for the IDispatch-based IShellWindows interface.</summary>
	internal sealed unsafe partial class ShellWindowsAutomation : IDisposable
	{
		private const int QueryInterfaceSlot = 0;
		private const int ReleaseSlot = 2;
		private const int QueryServiceSlot = 3;
		private const int BrowseObjectSlot = 11;
		private const int QueryActiveShellViewSlot = 15;
		private const int GetFolderSlot = 5;
		private const int GetCurrentFolderSlot = 5;

		private static readonly Guid ShellWindowsClassId = new("9BA05972-F6A8-11CF-A442-00A0C90A8F39");
		private static readonly Guid ServiceProviderInterfaceId = new("6D5140C1-7436-11CE-8034-00AA006009FA");
		private static readonly Guid TopLevelBrowserServiceId = new("4C96BE40-915C-11CF-99D3-00AA004AE837");
		private static readonly Guid ShellBrowserInterfaceId = new("000214E2-0000-0000-C000-000000000046");
		private static readonly Guid FolderViewInterfaceId = new("CDE725B0-CCC9-4519-917E-325D72FAB4CE");
		private static readonly Guid PersistFolder2InterfaceId = new("1AC3D9F0-175C-11D1-95BE-00609797EA4F");

		private IShellWindows? shellWindows;

		public ShellWindowsAutomation()
		{
			PInvoke.CoCreateInstance(ShellWindowsClassId, null, CLSCTX.CLSCTX_LOCAL_SERVER, out shellWindows).ThrowOnFailure();
			if (shellWindows is null)
				throw new InvalidOperationException("The shell did not return an IShellWindows instance.");
		}

		public int Count
		{
			get
			{
				ObjectDisposedException.ThrowIf(shellWindows is null, this);
				shellWindows.get_Count(out int count).ThrowOnFailure();
				return count;
			}
		}

		public ShellWindow? GetWindow(int index)
		{
			ObjectDisposedException.ThrowIf(shellWindows is null, this);
			ComVariant itemIndex = ComVariant.Create(index);
			HRESULT result = shellWindows.Item(itemIndex, out IDispatch? window);
			if (result.Failed || window is null)
				return null;

			void* windowPointer = ComInterfaceMarshaller<IDispatch>.ConvertToUnmanaged(window);
			return windowPointer is null ? null : new(windowPointer);
		}

		public void Dispose()
		{
			shellWindows = null;
			GC.SuppressFinalize(this);
		}

		private static void** GetVtable(void* instance)
			=> *(void***)instance;

		private static HRESULT QueryInterface(void* instance, Guid interfaceId, out void* result)
		{
			var queryInterface = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)GetVtable(instance)[QueryInterfaceSlot];
			Guid requestedInterface = interfaceId;
			void* queriedInterface = null;
			HRESULT queryResult = new(queryInterface(instance, &requestedInterface, &queriedInterface));
			result = queriedInterface;
			return queryResult;
		}

		private static void Release(ref void* instance)
		{
			if (instance is null)
				return;

			var release = (delegate* unmanaged[MemberFunction]<void*, uint>)GetVtable(instance)[ReleaseSlot];
			release(instance);
			instance = null;
		}

		internal sealed unsafe partial class ShellWindow : IDisposable
		{
			private void* window;

			public ShellWindow(void* window)
			{
				this.window = window;
			}

			public bool TryNavigate(ShellPidl targetPidl, ShellPidl controlPanelPidl)
			{
				ObjectDisposedException.ThrowIf(window is null, this);
				void* serviceProvider = null;
				void* shellBrowser = null;
				void* shellView = null;
				void* folderView = null;
				void* persistFolder = null;
				ITEMIDLIST* currentFolderPidl = null;
				byte[] targetBytes = targetPidl.GetBytes();
				byte[] controlPanelBytes = controlPanelPidl.GetBytes();

				fixed (byte* target = targetBytes)
				fixed (byte* controlPanel = controlPanelBytes)
				{
					try
					{
						if (QueryInterface(window, ServiceProviderInterfaceId, out serviceProvider).Failed ||
							serviceProvider is null)
							return false;
						if (QueryService(serviceProvider, TopLevelBrowserServiceId, ShellBrowserInterfaceId, out shellBrowser).Failed ||
							shellBrowser is null)
							return false;
						if (QueryActiveShellView(shellBrowser, out shellView).Failed || shellView is null)
							return false;
						if (QueryInterface(shellView, FolderViewInterfaceId, out folderView).Failed || folderView is null)
							return false;
						if (GetFolder(folderView, PersistFolder2InterfaceId, out persistFolder).Failed ||
							persistFolder is null)
							return false;
						if (GetCurrentFolder(persistFolder, out currentFolderPidl).Failed || currentFolderPidl is null)
							return false;

						bool canReuse = PInvoke.ILIsParent(currentFolderPidl, (ITEMIDLIST*)target, true) ||
							PInvoke.ILIsEqual(currentFolderPidl, (ITEMIDLIST*)controlPanel);
						return canReuse && BrowseObject(shellBrowser, (ITEMIDLIST*)target, PInvoke.SBSP_SAMEBROWSER | PInvoke.SBSP_ABSOLUTE).Succeeded;
					}
					finally
					{
						PInvoke.CoTaskMemFree(currentFolderPidl);
						Release(ref persistFolder);
						Release(ref folderView);
						Release(ref shellView);
						Release(ref shellBrowser);
						Release(ref serviceProvider);
					}
				}
			}

			public void Dispose()
			{
				Release(ref window);
				GC.SuppressFinalize(this);
			}

			private static HRESULT QueryService(void* serviceProvider, Guid serviceId, Guid interfaceId, out void* result)
			{
				var queryService = (delegate* unmanaged[MemberFunction]<void*, Guid*, Guid*, void**, int>)GetVtable(serviceProvider)[QueryServiceSlot];
				Guid requestedService = serviceId;
				Guid requestedInterface = interfaceId;
				void* service = null;
				HRESULT queryResult = new(queryService(serviceProvider, &requestedService, &requestedInterface, &service));
				result = service;
				return queryResult;
			}

			private static HRESULT QueryActiveShellView(void* shellBrowser, out void* shellView)
			{
				var queryActiveShellView = (delegate* unmanaged[MemberFunction]<void*, void**, int>)GetVtable(shellBrowser)[QueryActiveShellViewSlot];
				void* activeView = null;
				HRESULT queryResult = new(queryActiveShellView(shellBrowser, &activeView));
				shellView = activeView;
				return queryResult;
			}

			private static HRESULT GetFolder(void* folderView, Guid interfaceId, out void* folder)
			{
				var getFolder = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)GetVtable(folderView)[GetFolderSlot];
				Guid requestedInterface = interfaceId;
				void* shellFolder = null;
				HRESULT getFolderResult = new(getFolder(folderView, &requestedInterface, &shellFolder));
				folder = shellFolder;
				return getFolderResult;
			}

			private static HRESULT GetCurrentFolder(void* persistFolder, out ITEMIDLIST* folderPidl)
			{
				var getCurrentFolder = (delegate* unmanaged[MemberFunction]<void*, ITEMIDLIST**, int>)GetVtable(persistFolder)[GetCurrentFolderSlot];
				ITEMIDLIST* currentFolder = null;
				HRESULT getFolderResult = new(getCurrentFolder(persistFolder, &currentFolder));
				folderPidl = currentFolder;
				return getFolderResult;
			}

			private static HRESULT BrowseObject(void* shellBrowser, ITEMIDLIST* pidl, uint flags)
			{
				var browseObject = (delegate* unmanaged[MemberFunction]<void*, ITEMIDLIST*, uint, int>)GetVtable(shellBrowser)[BrowseObjectSlot];
				return new(browseObject(shellBrowser, pidl, flags));
			}
		}
	}
}
