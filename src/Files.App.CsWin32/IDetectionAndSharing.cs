// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Attributes;
using System;
using Windows.Win32.Foundation;

namespace Windows.Win32.UI.Shell
{
	public unsafe partial struct IDetectionAndSharing : IComIID
	{
		[GeneratedVTableFunction(Index = 3)]
		public partial HRESULT GetStatus(DTSH_TYPE type, DTSH_STATE* state, DTSH_ACTION* action);

		[GuidRVAGen.Guid("1FDA955C-61FF-11DA-978C-0008744FAAB7")]
		public static partial ref readonly Guid Guid { get; }
	}

	public enum DTSH_TYPE
	{
		DTSH_NETWORK_DISCOVERY = 0,
		DTSH_FILE_SHARING = 1,
	}

	public enum DTSH_STATE
	{
		DTSH_OFF = 0,
		DTSH_ON = 1,
	}

	public enum DTSH_ACTION
	{
		DTSH_NONE = 0,
	}
}
