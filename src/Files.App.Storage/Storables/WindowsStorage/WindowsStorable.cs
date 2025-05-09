// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Storage
{
	public unsafe abstract class WindowsStorable : IWindowsStorable
	{
		public IShellItem* ThisPtr
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		public IContextMenu* ContextMenu
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		public string Id => this.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

		public string Name => this.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI);

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

		public static unsafe WindowsStorable? TryParse(ITEMIDLIST* pidl)
		{
			IShellItem* pShellItem = default;

			HRESULT hr = PInvoke.SHCreateItemFromIDList(pidl, IID.IID_IShellItem, (void**)&pShellItem);
			if (hr.ThrowIfFailedOnDebug().Failed || pShellItem is null)
				return null;

			return TryParse(pShellItem);
		}

		public static WindowsStorable? TryParse(IShellItem* pShellItem)
		{
			bool isFolder = pShellItem->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded && returnedAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

			return isFolder ? new WindowsFolder(pShellItem) : new WindowsFile(pShellItem);
		}

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

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Name);
		}

		/// <inheritdoc/>
		public virtual void Dispose()
		{
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
