// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using Windows.Win32.Foundation;

namespace Windows.Win32.System.WinRT;

public unsafe partial struct IAutomaticDestinationListItemInfo : IComIID
{
	[GeneratedVTableFunction(Index = 6)]
	public partial HRESULT get_UsagePoints(double* value);

	[GeneratedVTableFunction(Index = 7)]
	public partial HRESULT put_UsagePoints(double value);

	[GeneratedVTableFunction(Index = 8)]
	public partial HRESULT get_LastUsed(long* value);

	[GeneratedVTableFunction(Index = 9)]
	public partial HRESULT put_LastUsed(long value);

	[GeneratedVTableFunction(Index = 10)]
	public partial HRESULT get_ActionCount(uint* value);

	[GeneratedVTableFunction(Index = 11)]
	public partial HRESULT put_ActionCount(uint value);

	[GuidRVAGen.Guid("96AD31E7-192B-5D9E-B84F-DCC1553BC5D9")]
	public static partial ref readonly Guid Guid { get; }
}
