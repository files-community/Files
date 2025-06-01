// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Storage
{
	public abstract class WindowsStorable : IWindowsStorable
	{
		public ComPtr<IShellItem> ThisPtr { get; protected set; }

		public string Id => this.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

		public string Name => this.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI);

		public static unsafe WindowsStorable? TryParse(string parsablePath)
		{
			HRESULT hr = default;
			IShellItem* pShellItem = default;

			fixed (char* pszParsablePath = parsablePath)
			{
				hr = PInvoke.SHCreateItemFromParsingName(
					pszParsablePath,
					null,
					IID.IID_IShellItem,
					(void**)&pShellItem);
			}

			if (hr.ThrowIfFailedOnDebug().Failed)
				return null;

			bool isFolder =
				pShellItem->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
				returnedAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

			return isFolder ? new WindowsFolder(pShellItem) : new WindowsFile(pShellItem);
		}

		public static unsafe WindowsStorable? TryParse(IShellItem* ptr)
		{
			bool isFolder =
				ptr->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
				returnedAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

			return isFolder ? new WindowsFolder(ptr) : new WindowsFile(ptr);
		}

		public static unsafe WindowsStorable? TryParse(ITEMIDLIST* pidl)
		{
			IShellItem* pShellItem = default;
			HRESULT hr = PInvoke.SHCreateItemFromIDList(pidl, IID.IID_IShellItem, (void**)&pShellItem);
			if (hr.ThrowIfFailedOnDebug().Failed || pShellItem is null)
				return null;

			bool isFolder =
				pShellItem->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
				returnedAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

			return isFolder ? new WindowsFolder(pShellItem) : new WindowsFile(pShellItem);
		}

		public unsafe Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			IShellItem* pParentFolder = default;
			HRESULT hr = ThisPtr.Get()->GetParent(&pParentFolder);
			if (hr.Failed)
			{
				if (pParentFolder is not null) pParentFolder->Release();
				return Task.FromResult<IFolder?>(null);
			}

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
		public void Dispose()
		{
			ThisPtr.Dispose();
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

			return ThisPtr.Get()->Compare(other.ThisPtr.Get(), (uint)_SICHINTF.SICHINT_DISPLAY, out int order).Succeeded && order is 0;
		}

		public static bool operator ==(WindowsStorable left, WindowsStorable right)
			=> left.Equals(right);

		public static bool operator !=(WindowsStorable left, WindowsStorable right)
			=> !(left == right);
	}
}
