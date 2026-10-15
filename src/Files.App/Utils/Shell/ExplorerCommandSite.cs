// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using ComIServiceProvider = Windows.Win32.System.Com.IServiceProvider;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// The site File Explorer gives to the Windows 11 context menu commands (<see cref="IExplorerCommand"/>):
	/// a service provider whose folder view reports the folder the menu was opened in. Some commands, such as
	/// PowerToys New+, read the target folder from it instead of from the selection.
	/// </summary>
	/// <remarks>
	/// The commands run out of process, so they reach this object through COM proxies; only
	/// <see cref="IFolderView.GetFolder"/> is implemented.
	/// </remarks>
	[GeneratedComClass]
	internal sealed partial class ExplorerCommandSite : ComIServiceProvider, IFolderView
	{
		private readonly IShellFolder folder;

		private ExplorerCommandSite(IShellFolder folder)
		{
			this.folder = folder;
		}

		/// <summary>
		/// Creates the site for the given folder; call it on the thread that uses the commands.
		/// </summary>
		public static ExplorerCommandSite? Create(string folderPath)
		{
			try
			{
				using var item = ShellFolderExtensions.GetShellItemFromPathOrPIDL(folderPath);
				return item.IShellItem.BindToHandler(null, PInvoke.BHID_SFObject, out IShellFolder? folder).Succeeded && folder is not null
					? new(folder)
					: null;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return null;
			}
		}

		/// <summary>
		/// Releases the folder; call it on the thread the site was created on.
		/// </summary>
		public void Release()
			=> ((object)folder as ComObject)?.FinalRelease();

		public unsafe HRESULT QueryService(Guid* guidService, Guid* riid, out object ppvObject)
		{
			// SID_SFolderView is the IID of IFolderView. The proxy queries the returned object for riid.
			if (*guidService == typeof(IFolderView).GUID)
			{
				ppvObject = this;
				return HRESULT.S_OK;
			}

			ppvObject = null!;
			return HRESULT.E_NOINTERFACE;
		}

		public unsafe HRESULT GetFolder(Guid* riid, out object ppv)
		{
			ppv = folder;
			return HRESULT.S_OK;
		}

		public HRESULT GetCurrentViewMode(out uint pViewMode)
		{
			pViewMode = 0;
			return HRESULT.E_NOTIMPL;
		}

		public HRESULT SetCurrentViewMode(uint ViewMode)
			=> HRESULT.E_NOTIMPL;

		public unsafe HRESULT Item(int iItemIndex, ITEMIDLIST** ppidl)
		{
			*ppidl = null;
			return HRESULT.E_NOTIMPL;
		}

		public HRESULT ItemCount(_SVGIO uFlags, out int pcItems)
		{
			pcItems = 0;
			return HRESULT.E_NOTIMPL;
		}

		public unsafe HRESULT Items(_SVGIO uFlags, Guid* riid, out object ppv)
		{
			ppv = null!;
			return HRESULT.E_NOTIMPL;
		}

		public HRESULT GetSelectionMarkedItem(out int piItem)
		{
			piItem = -1;
			return HRESULT.E_NOTIMPL;
		}

		public HRESULT GetFocusedItem(out int piItem)
		{
			piItem = -1;
			return HRESULT.E_NOTIMPL;
		}

		public unsafe HRESULT GetItemPosition(ITEMIDLIST* pidl, System.Drawing.Point* ppt)
			=> HRESULT.E_NOTIMPL;

		public unsafe HRESULT GetSpacing(System.Drawing.Point* ppt)
			=> HRESULT.E_NOTIMPL;

		public unsafe HRESULT GetDefaultSpacing(System.Drawing.Point* ppt)
			=> HRESULT.E_NOTIMPL;

		public HRESULT GetAutoArrange()
			=> HRESULT.S_FALSE;

		public HRESULT SelectItem(int iItem, uint dwFlags)
			=> HRESULT.E_NOTIMPL;

		public unsafe HRESULT SelectAndPositionItems(uint cidl, ITEMIDLIST** apidl, System.Drawing.Point* apt, uint dwFlags)
			=> HRESULT.E_NOTIMPL;
	}
}
