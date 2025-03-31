// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Storage
{
	public unsafe class JumpListManager : IDisposable
	{
		private ComPtr<ICustomDestinationList> pCustomDestinationList = default;

		private static string? AppId
		{
			get
			{
				PWSTR pszAppId = default;
				HRESULT hr = PInvoke.GetCurrentProcessExplicitAppUserModelID(&pszAppId);
				if (hr == HRESULT.E_FAIL)
					hr = HRESULT.S_OK;

				hr.ThrowIfFailedOnDebug();

				return pszAppId.ToString();
			}
		}

		public ConcurrentBag<JumpListItem> JumpListItems { get; private set; } = [];

		public ConcurrentBag<JumpListItem> RemovedItems { get; private set; } = [];

		public ConcurrentBag<JumpListItem> RejectedItems { get; private set; } = [];

		// A special "Frequent" category managed by Windows
		public bool ShowFrequentCategory { get; set; }

		// A special "Recent" category managed by Windows
		public bool ShowRecentCategory { get; set; }

		private static JumpListManager? _Default = null;
		public static JumpListManager Default { get; } = _Default ??= new JumpListManager();

		public JumpListManager()
		{
			Guid CLSID_CustomDestinationList = typeof(DestinationList).GUID;
			Guid IID_ICustomDestinationList = ICustomDestinationList.IID_Guid;
			HRESULT hr = PInvoke.CoCreateInstance(
				&CLSID_CustomDestinationList,
				null,
				CLSCTX.CLSCTX_INPROC_SERVER,
				&IID_ICustomDestinationList,
				(void**)pCustomDestinationList.GetAddressOf());

			// Should not happen but as a sanity check at an early stage
			hr.ThrowOnFailure();
		}

		public HRESULT Save()
		{
			Debug.Assert(Thread.CurrentThread.GetApartmentState() is ApartmentState.STA);

			HRESULT hr = pCustomDestinationList.Get()->SetAppID(AppId);

			uint cMinSlots = 0;
			ComPtr<IObjectArray> pDeletedItemsObjectArray = default;
			Guid IID_IObjectArray = IObjectArray.IID_Guid;

			hr = pCustomDestinationList.Get()->BeginList(&cMinSlots, &IID_IObjectArray, (void**)pDeletedItemsObjectArray.GetAddressOf());

			// TODO: Validate items

			// TODO: Group them as categories

			// TODO: Append a custom category or to the Tasks

			if (ShowFrequentCategory)
				pCustomDestinationList.Get()->AppendKnownCategory(KNOWNDESTCATEGORY.KDC_FREQUENT);

			if (ShowRecentCategory)
				pCustomDestinationList.Get()->AppendKnownCategory(KNOWNDESTCATEGORY.KDC_RECENT);

			return HRESULT.S_OK;
		}

		public void Dispose()
		{
			pCustomDestinationList.Dispose();
		}
	}
}
