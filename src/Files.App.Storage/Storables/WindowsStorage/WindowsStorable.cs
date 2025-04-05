// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	public abstract class WindowsStorable : IStorableChild
	{
		public ComPtr<IShellItem> ThisPtr { get; protected set; }

		public string Id => throw new NotImplementedException();

		public string Name => throw new NotImplementedException();

		public static unsafe bool TryParse(string parsablePath, out WindowsStorable? windowsStorable)
		{
			HRESULT hr = default;
			windowsStorable = default;

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
				return false;

			var pShellFolder = pShellItem.As<IShellFolder>();
			windowsStorable = pShellFolder.IsNull ? new WindowsFile(pShellItem) : new WindowsFolder(pShellItem);
			pShellFolder.Dispose();

			return true;
		}

		public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			throw new NotImplementedException();
		}
	}
}
