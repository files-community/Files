// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Storage.Storables
{
	/// <summary>
	/// Represents a folder object that is natively supported by Windows Shell API.
	/// </summary>
	public class NativeFolder : NativeStorable/*, IFolder*/
    {
		public async IAsyncEnumerable<IStorable> GetChildrenAsync()
		{
			foreach (var storable in GetChildren())
			{
				await Task.Yield();

				yield return storable;
			}

			unsafe IEnumerable<NativeStorable> GetChildren()
			{
				using ComPtr<IEnumShellItems> pEnumShellItems = default;
				fixed (Guid* pBHID = PInvoke.BHID_EnumItems)
				{
					hr = pRecycleBinFolderShellItem.Get()->BindToHandler(
						null,
						pBHID,
						(Guid*)Unsafe.AsPointer(ref Unsafe.AsRef(in IEnumShellItems.Guid)),
						(void**)pEnumShellItems.GetAddressOf());

						ComPtr<IShellItem> pShellItem = default;
						while (pEnumShellItems.Get()->Next(1, pShellItem.GetAddressOf()) == HRESULT.S_OK)
							yield return NativeStorable(pShellItem);
				}
			}
		}
	}
}
