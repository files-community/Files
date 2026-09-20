// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Principal;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.Networking.ActiveDirectory;
using Windows.Win32.System.Variant;

namespace Files.App.Storage
{
	public static unsafe class WindowsObjectPicker
	{
		private const string ObjectSidAttributeName = "objectSid";

		public static Task<string?> OpenObjectPickerAsync(nint ownerHWnd, ILogger? logger)
			=> STATask.Run(() => OpenObjectPicker((HWND)ownerHWnd), logger);

		private static string? OpenObjectPicker(HWND ownerHWnd)
		{
			Guid CLSID_DsObjectPicker = PInvoke.CLSID_DsObjectPicker;

			HRESULT hr = PInvoke.CoCreateInstance(CLSID_DsObjectPicker, null, CLSCTX.CLSCTX_INPROC_SERVER, out IDsObjectPicker? pObjectPicker);
			if (hr.ThrowIfFailedOnDebug().Failed || pObjectPicker is null)
				return null;

			DSOP_SCOPE_INIT_INFO* scopeInitInfos = stackalloc DSOP_SCOPE_INIT_INFO[2];
			scopeInitInfos[0] = CreateScopeInitInfo(PInvoke.DSOP_SCOPE_TYPE_TARGET_COMPUTER, true);
			scopeInitInfos[1] = CreateScopeInitInfo(
				PInvoke.DSOP_SCOPE_TYPE_UPLEVEL_JOINED_DOMAIN |
				PInvoke.DSOP_SCOPE_TYPE_DOWNLEVEL_JOINED_DOMAIN |
				PInvoke.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN |
				PInvoke.DSOP_SCOPE_TYPE_GLOBAL_CATALOG |
				PInvoke.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN |
				PInvoke.DSOP_SCOPE_TYPE_EXTERNAL_DOWNLEVEL_DOMAIN |
				PInvoke.DSOP_SCOPE_TYPE_WORKGROUP |
				PInvoke.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE |
				PInvoke.DSOP_SCOPE_TYPE_USER_ENTERED_DOWNLEVEL_SCOPE,
				false);

			fixed (char* pszObjectSidAttributeName = ObjectSidAttributeName)
			{
				PCWSTR* attributeNames = stackalloc PCWSTR[1];
				attributeNames[0] = pszObjectSidAttributeName;

				DSOP_INIT_INFO initInfo = default;
				initInfo.cbSize = (uint)sizeof(DSOP_INIT_INFO);
				initInfo.cDsScopeInfos = 2;
				initInfo.aDsScopeInfos = scopeInitInfos;
				initInfo.cAttributesToFetch = 1;
				initInfo.apwzAttributeNames = attributeNames;

				hr = pObjectPicker.Initialize(ref initInfo);
				if (hr.ThrowIfFailedOnDebug().Failed)
					return null;
			}

			hr = pObjectPicker.InvokeDialog(ownerHWnd, out IDataObject pSelections);
			if (hr == HRESULT.S_FALSE || hr.ThrowIfFailedOnDebug().Failed)
				return null;

			return GetSelectedSid(pSelections);
		}

		private static DSOP_SCOPE_INIT_INFO CreateScopeInitInfo(uint scopeType, bool startingScope)
		{
			const uint defaultFilter =
				PInvoke.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS |
				PInvoke.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS;

			const uint upLevelFilter =
				PInvoke.DSOP_FILTER_INCLUDE_ADVANCED_VIEW |
				PInvoke.DSOP_FILTER_USERS |
				PInvoke.DSOP_FILTER_BUILTIN_GROUPS |
				PInvoke.DSOP_FILTER_WELL_KNOWN_PRINCIPALS |
				PInvoke.DSOP_FILTER_UNIVERSAL_GROUPS_DL |
				PInvoke.DSOP_FILTER_UNIVERSAL_GROUPS_SE |
				PInvoke.DSOP_FILTER_GLOBAL_GROUPS_DL |
				PInvoke.DSOP_FILTER_GLOBAL_GROUPS_SE |
				PInvoke.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_DL |
				PInvoke.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE |
				PInvoke.DSOP_FILTER_CONTACTS |
				PInvoke.DSOP_FILTER_COMPUTERS |
				PInvoke.DSOP_FILTER_SERVICE_ACCOUNTS;

			const uint downLevelFilter =
				PInvoke.DSOP_DOWNLEVEL_FILTER_USERS |
				PInvoke.DSOP_DOWNLEVEL_FILTER_LOCAL_GROUPS |
				PInvoke.DSOP_DOWNLEVEL_FILTER_GLOBAL_GROUPS |
				PInvoke.DSOP_DOWNLEVEL_FILTER_COMPUTERS |
				PInvoke.DSOP_DOWNLEVEL_FILTER_ALL_WELLKNOWN_SIDS;

			DSOP_SCOPE_INIT_INFO info = default;
			info.cbSize = (uint)sizeof(DSOP_SCOPE_INIT_INFO);
			info.flType = scopeType;
			info.flScope = startingScope
				? PInvoke.DSOP_SCOPE_FLAG_STARTING_SCOPE | defaultFilter
				: defaultFilter;
			info.FilterFlags.Uplevel.flBothModes = upLevelFilter;
			info.FilterFlags.flDownlevel = downLevelFilter;

			return info;
		}

		private static string? GetSelectedSid(IDataObject pSelections)
		{
			uint clipboardFormat = PInvoke.RegisterClipboardFormat(PInvoke.CFSTR_DSOP_DS_SELECTION_LIST);

			if (clipboardFormat is 0 || clipboardFormat > ushort.MaxValue)
				return null;

			FORMATETC format = default;
			format.cfFormat = (ushort)clipboardFormat;
			format.dwAspect = (uint)DVASPECT.DVASPECT_CONTENT;
			format.lindex = -1;
			format.tymed = (uint)TYMED.TYMED_HGLOBAL;

			STGMEDIUM medium = default;
			medium.tymed = TYMED.TYMED_HGLOBAL;

			HRESULT hr = pSelections.GetData(format, out medium);
			if (hr.ThrowIfFailedOnDebug().Failed)
				return null;

			void* pvSelectionList = PInvoke.GlobalLock(medium.u.hGlobal);
			if (pvSelectionList is null)
			{
				PInvoke.ReleaseStgMedium(ref medium);
				return null;
			}

			try
			{
				DS_SELECTION_LIST* pSelectionList = (DS_SELECTION_LIST*)pvSelectionList;
				if (pSelectionList->cItems is 0 || pSelectionList->cFetchedAttributes is 0)
					return null;

				var selectionsAsSpan = pSelectionList->aDsSelection.AsSpan((int)pSelectionList->cItems);
				fixed (DS_SELECTION_unmanaged* pSelection = selectionsAsSpan)
				{
					if (pSelection->pvarFetchedAttributes is null)
						return null;

					return GetSidString(pSelection->pvarFetchedAttributes);
				}
			}
			finally
			{
				PInvoke.GlobalUnlock(medium.u.hGlobal);
				PInvoke.ReleaseStgMedium(ref medium);
			}
		}

		private static string? GetSidString(ComVariant* objectSidVariant)
		{
			VarEnum variantType = objectSidVariant->VarType;
			if ((variantType & VarEnum.VT_ARRAY) is 0 || (variantType & (VarEnum)0x0FFF) is not VarEnum.VT_UI1)
				return null;

			ref nint rawValue = ref objectSidVariant->GetRawDataRef<nint>();
			SAFEARRAY* pSafeArray = (variantType & VarEnum.VT_BYREF) is not 0
				? *(SAFEARRAY**)rawValue
				: (SAFEARRAY*)rawValue;

			if (pSafeArray is null || PInvoke.SafeArrayGetDim(pSafeArray) is not 1)
				return null;

			int lowerBound = 0, upperBound = 0;
			if (PInvoke.SafeArrayGetLBound(pSafeArray, 1, out lowerBound).ThrowIfFailedOnDebug().Failed ||
				PInvoke.SafeArrayGetUBound(pSafeArray, 1, out upperBound).ThrowIfFailedOnDebug().Failed ||
				upperBound < lowerBound)
				return null;

			if (PInvoke.SafeArrayAccessData(pSafeArray, out void* pSidBytes).ThrowIfFailedOnDebug().Failed || pSidBytes is null)
				return null;

			try
			{
				int length = upperBound - lowerBound + 1;
				byte[] sidBytes = new ReadOnlySpan<byte>((byte*)pSidBytes, length).ToArray();

				return new SecurityIdentifier(sidBytes, 0).Value;
			}
			finally
			{
				PInvoke.SafeArrayUnaccessData(pSafeArray);
			}
		}
	}
}
