// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Files.Shared.Attributes;
using System;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Windows.Win32.UI.Shell
{
	public unsafe partial struct IOpenControlPanel : IComIID
	{
		[GeneratedVTableFunction(Index = 3)]
		public partial HRESULT Open(char* name, char* page, void* site);

		[GuidRVAGen.Guid("D11AD862-66DE-4DF4-BF6C-1F5621996AF1")]
		public static partial ref readonly Guid Guid { get; }
	}
}
