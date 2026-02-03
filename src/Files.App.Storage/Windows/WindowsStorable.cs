// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public unsafe abstract class WindowsStorable : IWindowsStorable
	{
		private readonly IGlobalInterfaceTable* _globalInterfaceTable;
		private uint _gitCookieForThisPtr = 0U;
		private uint _gitCookieForContextMenu = 0U;

		/// <inheritdoc/>
		public IShellItem* ThisPtr
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				void* pv;
				HRESULT hr = _globalInterfaceTable->GetInterfaceFromGlobal(_gitCookieForThisPtr, IID.IID_IShellItem, &pv);
				return (IShellItem*)pv;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected set
			{
				uint cookie;
				HRESULT hr = _globalInterfaceTable->RegisterInterfaceInGlobal((IUnknown*)value, IID.IID_IShellItem, &cookie);
				_gitCookieForThisPtr = cookie;
			}
		}

		/// <inheritdoc/>
		public IContextMenu* ContextMenu
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				void* pv;
				HRESULT hr = _globalInterfaceTable->GetInterfaceFromGlobal(_gitCookieForContextMenu, IID.IID_IContextMenu, &pv);
				return (IContextMenu*)pv;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected set
			{
				uint cookie;
				HRESULT hr = _globalInterfaceTable->RegisterInterfaceInGlobal((IUnknown*)value, IID.IID_IContextMenu, &cookie);
				_gitCookieForContextMenu = cookie;
			}
		}

		/// <inheritdoc/>
		public string Id => this.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

		/// <inheritdoc/>
		public string Name => this.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI);

		public WindowsStorable()
		{
			void* globalInterfaceTable;
			PInvoke.CoCreateInstance(CLSID.CLSID_StdGlobalInterfaceTable, null, CLSCTX.CLSCTX_INPROC_SERVER, IID.IID_IGlobalInterfaceTable, &globalInterfaceTable);
			_globalInterfaceTable = (IGlobalInterfaceTable*)globalInterfaceTable;
		}

		public static WindowsStorable? TryParse(string szPath)
		{
			HRESULT hr = default;
			IShellItem* pShellItem = null;

			fixed (char* pszPath = szPath)
				hr = PInvoke.SHCreateItemFromParsingName(pszPath, null, IID.IID_IShellItem, (void**)&pShellItem);

			if (pShellItem is null)
				return null;

			return TryParse(pShellItem);
		}

		public static WindowsStorable? TryParse(IShellItem* pShellItem)
		{
			bool isFolder = pShellItem->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded && returnedAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

			return isFolder ? new WindowsFolder(pShellItem) : new WindowsFile(pShellItem);
		}

		/// <inheritdoc/>
		public unsafe Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			IShellItem* pParentFolder = default;
			HRESULT hr = ThisPtr->GetParent(&pParentFolder);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return Task.FromResult<IFolder?>(null);

			return Task.FromResult<IFolder?>(new WindowsFolder(pParentFolder));
		}

		/// <inheritdoc/>
		public override bool Equals(object? obj)
		{
			return Equals(obj as IWindowsStorable);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Name);
		}

		/// <inheritdoc/>
		public virtual void Dispose()
		{
			_globalInterfaceTable->Release();
			if (ThisPtr is not null) ThisPtr->Release();
			if (ContextMenu is not null) ContextMenu->Release();
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return this.GetDisplayName();
		}

		/// <inheritdoc/>
		public unsafe bool Equals(IWindowsStorable? other)
		{
			if (other is null)
				return false;

			return ThisPtr->Compare(other.ThisPtr, (uint)_SICHINTF.SICHINT_DISPLAY, out int order).Succeeded && order is 0;
		}

		public static bool operator ==(WindowsStorable left, WindowsStorable right)
			=> left.Equals(right);

		public static bool operator !=(WindowsStorable left, WindowsStorable right)
			=> !(left == right);
	}
}
