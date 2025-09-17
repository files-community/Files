// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe partial struct IStorageProviderQuotaUI : IComIID
	{
		[GeneratedVTableFunction(Index = 6)]
		public partial HRESULT GetQuotaTotalInBytes(ulong* value);

		[GeneratedVTableFunction(Index = 8)]
		public partial HRESULT GetQuotaUsedInBytes(ulong* value);

		[GuidRVAGen.Guid("BA6295C3-312E-544F-9FD5-1F81B21F3649")]
		public static partial ref readonly Guid Guid { get; }
	}
}
