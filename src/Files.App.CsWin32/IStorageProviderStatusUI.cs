// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe partial struct IStorageProviderStatusUI : IComIID
	{
		[GeneratedVTableFunction(Index = 14)]
		public partial HRESULT GetQuotaUI(IStorageProviderQuotaUI** result);

		[GuidRVAGen.Guid("D6B6A758-198D-5B80-977F-5FF73DA33118")]
		public static partial ref readonly Guid Guid { get; }
	}
}
