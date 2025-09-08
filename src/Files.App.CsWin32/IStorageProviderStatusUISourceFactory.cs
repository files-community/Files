// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT
{
	public unsafe partial struct IStorageProviderStatusUISourceFactory : IComIID
	{
		[GeneratedVTableFunction(Index = 6)]
		public partial HRESULT GetStatusUISource(nint syncRootId, IStorageProviderStatusUISource** result);

		[GuidRVAGen.Guid("12E46B74-4E5A-58D1-A62F-0376E8EE7DD8")]
		public static partial ref readonly Guid Guid { get; }
	}
}
