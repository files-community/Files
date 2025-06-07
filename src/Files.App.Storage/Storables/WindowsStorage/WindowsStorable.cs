// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public unsafe abstract class WindowsStorable : IWindowsStorable, IStorableChild, IEquatable<IWindowsStorable>
	{
		public IShellItem* ThisPtr
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected set;
		}

		public string Id
			=> this.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

		public string Name
			=> this.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEFORUI);

		public static WindowsStorable? TryParse(string parsablePath)
		{
			HRESULT hr = default;
			IShellItem* pShellItem = default;

			fixed (char* pszParsablePath = parsablePath)
				hr = PInvoke.SHCreateItemFromParsingName(pszParsablePath, null, IID.IID_IShellItem, (void**)&pShellItem);

			if (pShellItem is null)
				return null;

			bool isFolder = pShellItem->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
				returnedAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

			return isFolder ? new WindowsFolder(pShellItem) : new WindowsFile(pShellItem);
		}

		public static WindowsStorable? TryParse(IShellItem* ptr)
		{
			bool isFolder = ptr->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var returnedAttributes).Succeeded &&
				returnedAttributes is SFGAO_FLAGS.SFGAO_FOLDER;

			return isFolder ? new WindowsFolder(ptr) : new WindowsFile(ptr);
		}

		public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			ComPtr<IShellItem> pParentFolder = default;
			HRESULT hr = ThisPtr->GetParent(pParentFolder.GetAddressOf());
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
			ThisPtr->Release();
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
