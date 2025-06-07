// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;

namespace Files.App.Storage
{
	public unsafe static partial class WindowsStorageHelpers
	{
		public static bool OpenRegistryKey(HKEY hRootKey, string subKey, REG_SAM_FLAGS flags, out HKEY hKey)
		{
			WIN32_ERROR dwResult = default;
			HKEY hResultKey = default;

			fixed (char* pszSubKey = subKey)
				dwResult = PInvoke.RegOpenKeyEx(hRootKey, pszSubKey, 0, flags, &hResultKey);

			hKey = hResultKey;

			return dwResult is WIN32_ERROR.ERROR_SUCCESS;
		}

		public static bool GetRegistryValue<T>(HKEY hRootKey, string szSubKey, string szValueName, REG_ROUTINE_FLAGS dwRoutineFlags, out T? valueData)
		{
			valueData = default;

			WIN32_ERROR dwResult = default;
			HKEY hKey = hRootKey;

			if (!string.IsNullOrEmpty(szSubKey))
			{
				fixed (char* pszSubKey = szSubKey)
					dwResult = PInvoke.RegOpenKeyEx(hRootKey, pszSubKey, 0, REG_SAM_FLAGS.KEY_QUERY_VALUE, &hKey);

				if (dwResult is not WIN32_ERROR.ERROR_SUCCESS)
				{
					if (!hKey.IsNull) PInvoke.RegCloseKey(hKey);
					return false;
				}
			}

			REG_VALUE_TYPE dwValueType = default;
			byte* pData = null;
			uint cbData = 0U;

			fixed (char* pszValueName = szValueName)
			{
				dwResult = PInvoke.RegGetValue(hKey, default, pszValueName, dwRoutineFlags, null, null, &cbData);

				if (dwResult is WIN32_ERROR.ERROR_SUCCESS or WIN32_ERROR.ERROR_MORE_DATA)
				{
					if (cbData is 0U)
						return false;

					pData = (byte*)NativeMemory.Alloc(cbData);
					dwResult = PInvoke.RegGetValue(hKey, default, pszValueName, dwRoutineFlags, &dwValueType, pData, &cbData);

					switch (dwValueType)
					{
						default:
						case REG_VALUE_TYPE.REG_NONE:
						case REG_VALUE_TYPE.REG_BINARY:
							{
								byte[] byteArrayData = new byte[cbData];
								Marshal.Copy((nint)pData, byteArrayData, 0, (int)cbData);

								valueData = (T)(object)byteArrayData;
							}
							break;
						case REG_VALUE_TYPE.REG_DWORD:
						case REG_VALUE_TYPE.REG_QWORD:
							{
								valueData = cbData switch
								{
									4U => Unsafe.As<uint, T>(ref *(uint*)pData),
									8U => Unsafe.As<ulong, T>(ref *(ulong*)pData),
									_ => throw new InvalidCastException($"Registry value size of data \"{nameof(pData)}\" of \"{nameof(pszValueName)}\" is invalid (size: \"{cbData}\")."),
								};
							}
							break;
						case REG_VALUE_TYPE.REG_DWORD_BIG_ENDIAN:
							{
								var uint32Data = BinaryPrimitives.ReadUInt32BigEndian(new Span<byte>(pData, (int)cbData));
								valueData = Unsafe.As<uint, T>(ref uint32Data);
							}
							break;
						case REG_VALUE_TYPE.REG_SZ:
						case REG_VALUE_TYPE.REG_EXPAND_SZ:
							{
								valueData = (T)(object)new string((char*)pData);
							}
							break;
						case REG_VALUE_TYPE.REG_MULTI_SZ:
							{
								byte* pDataPtrSeeker = pData;
								uint dwSeparatorCount = 0U;
								uint dwArrayIndex = 0U;
								string[] stringDataArray = new string[dwSeparatorCount + 1];

								while (pDataPtrSeeker < pData + cbData)
								{
									if ((char)*pDataPtrSeeker is '\0')
										dwSeparatorCount++;

									pDataPtrSeeker++;
								}

								// Reset pointer to the start of the data
								pDataPtrSeeker = pData;

								while (pDataPtrSeeker < pData + cbData)
								{
									if ((char)*pDataPtrSeeker is '\0')
									{
										dwArrayIndex++;
										continue;
									}

									stringDataArray[dwArrayIndex] = new((char*)pData);
									pDataPtrSeeker += stringDataArray[dwArrayIndex].Length;
								}

								valueData = (T)(object)stringDataArray;
							}
							break;
					}

					NativeMemory.Free(pData);
				}
			}

			if (!hKey.IsNull) PInvoke.RegCloseKey(hKey);

			return dwResult is WIN32_ERROR.ERROR_SUCCESS;
		}

		public static string[] EnumerateRegistryKeyNames(HKEY hKey)
		{
			WIN32_ERROR dwResult = default;
			uint dwIndex = 0U;
			char* pszName = stackalloc char[256]; // 255 chars + null terminator
			uint cchName = 256U;
			string[] keyNames = [];

			while ((dwResult = PInvoke.RegEnumKeyEx(hKey, dwIndex, pszName, &cchName, null, null, null, null)) is not WIN32_ERROR.ERROR_NO_MORE_ITEMS)
			{
				if (dwResult is WIN32_ERROR.ERROR_SUCCESS)
				{
					// Double the capacity of the array if necessary
					if (dwIndex >= keyNames.Length)
						Array.Resize(ref keyNames, keyNames.Length < 10 ? 10 : keyNames.Length * 2);

					keyNames[dwIndex++] = new string(pszName);
				}
				else
				{
					// An error occurred, handle it accordingly
					return [];
				}
			}

			// Fit the array if necessary
			if (dwIndex < keyNames.Length)
				Array.Resize(ref keyNames, (int)dwIndex);

			return keyNames;
		}
	}
}
