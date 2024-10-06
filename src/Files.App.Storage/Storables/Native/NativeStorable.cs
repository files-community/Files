// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage.Storables
{
	/// <summary>
	/// Represents a storable that is natively supported by Windows Shell API.
	/// </summary>
	public abstract class NativeStorable : INativeStorable
	{
		/// <inheritdoc/>
		/// <remarks>
		/// This must be a path that can be parsed by SHCreateItemFromParsingName.
		/// </remarks>
		public string Path { get; protected set; }

		/// <inheritdoc/>
		/// <remarks>
		/// This must be a path that can be parsed by SHParseDisplayName.
		/// </remarks>
		public string Name { get; protected set; }

		/// <inheritdoc/>
		public string Id { get; protected set; } // Won't use

		protected ComPtr<IShellItem> m_pShellItem { get; private set; }

		/// <summary>
		/// Initializes an instance of <see cref="NativeStorable"/> class.
		/// </summary>
		/// <param name="path">Win32 file namespace, shell namespace, or UNC path.</param>
		public unsafe NativeStorable(string path)
		{
			HRESULT hr = PInvoke.SHCreateItemFromParsingName(
				path,
				null,
				(Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IShellItem.Guid)),
				(void**)m_pShellItem.GetAddressOf());
		}

		/// <summary>
		/// Initializes an instance of <see cref="NativeStorable"/> class.
		/// </summary>
		/// <param name="shellGuid">An instance of GUID that represents a shell folder.</param>
		public unsafe NativeStorable(Guid shellGuid)
		{
			HRESULT hr = default;

			// For known folders
			fixed (Guid* pFolderId = &shellGuid)
			{
				hr = PInvoke.SHGetKnownFolderItem(
					pFolderId,
					KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT,
					HANDLE.Null,
					(Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IShellItem.Guid)),
					(void**)m_pShellItem.GetAddressOf());
			}

			if (hr == HRESULT.S_OK)
				return;

			string path = $"Shell:::{shellGuid.ToString("B")}";

			hr = PInvoke.SHCreateItemFromParsingName(
				path,
				null,
				(Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IShellItem.Guid)),
				(void**)m_pShellItem.GetAddressOf());
		}

		/// <summary>
		/// Initializes an instance of <see cref="NativeStorable"/> class.
		/// </summary>
		/// <param name="pShellItem">An instance of <see cref="IShellItem"/>.</param>
		public NativeStorable(ComPtr<IShellItem> pShellItem)
		{
			m_pShellItem = pShellItem;
		}

		/// <inheritdoc/>
		public string GetPropertyAsync(string id)
		{
			using ComPtr<IShellItem2> pShellItem2 = default;
			hr = pShellItem.Get()->QueryInterface(
				(Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IShellItem2.Guid)),
				(void**)pShellItem2.GetAddressOf());

			hr = PInvoke.PSGetPropertyKeyFromName(
				id,
				out var propertyKey);

			using ComHeapPtr<LPWSTR> pszPropertyValue = default;
			//hr = pShellItem2.Get()->GetString(
			//	&propertyKey,
			//	pszPropertyValue.GetAddressOf());

			return pszPropertyValue.Get()->ToString();
		}
	}
}
