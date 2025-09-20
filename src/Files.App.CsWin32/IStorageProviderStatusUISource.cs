// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe partial struct IStorageProviderStatusUISource : IComIID
	{
		[GeneratedVTableFunction(Index = 6)]
		public partial HRESULT GetStatusUI(IStorageProviderStatusUI** result);

		[GuidRVAGen.Guid("A306C249-3D66-5E70-9007-E43DF96051FF")]
		public static partial ref readonly Guid Guid { get; }
	}
}
