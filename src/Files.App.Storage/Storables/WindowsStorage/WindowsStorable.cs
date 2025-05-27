// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

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
			ComPtr<IShellItem> pShellItem = default;
			var IID_IShellItem = typeof(IShellItem).GUID;

			fixed (char* pszParsablePath = parsablePath)
			{
				hr = PInvoke.SHCreateItemFromParsingName(
					pszParsablePath,
					null,
					&IID_IShellItem,
					(void**)pShellItem.GetAddressOf());
			}

			if (pShellItem.IsNull)
				return null;

			return pShellItem.HasShellAttributes(SFGAO_FLAGS.SFGAO_FOLDER)
				? new WindowsFolder(pShellItem)
				: new WindowsFile(pShellItem);
		}

		public static unsafe WindowsStorable? TryParse(IShellItem* ptr)
		{
			ComPtr<IShellItem> pShellItem = default;
			pShellItem.Attach(ptr);

			return pShellItem.HasShellAttributes(SFGAO_FLAGS.SFGAO_FOLDER)
				? new WindowsFolder(pShellItem)
				: new WindowsFile(pShellItem);
		}

		public unsafe Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			ComPtr<IShellItem> pParentFolder = default;
			HRESULT hr = ThisPtr.Get()->GetParent(pParentFolder.GetAddressOf());
			if (hr.Failed)
			{
				if (!pParentFolder.IsNull) pParentFolder.Dispose();

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
