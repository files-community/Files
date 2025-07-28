// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Files.App.Storage
{
	public unsafe class TaskbarManager : IDisposable
	{
		private ComPtr<ITaskbarList3> pTaskbarList = default;

		private static TaskbarManager? _Default = null;
		public static TaskbarManager Default { get; } = _Default ??= new TaskbarManager();

		public TaskbarManager()
		{
			Guid CLSID_TaskbarList = typeof(TaskbarList).GUID;
			Guid IID_ITaskbarList3 = ITaskbarList3.IID_Guid;
			HRESULT hr = PInvoke.CoCreateInstance(
				&CLSID_TaskbarList,
				null,
				CLSCTX.CLSCTX_INPROC_SERVER,
				&IID_ITaskbarList3,
				(void**)pTaskbarList.GetAddressOf());

			if (hr.ThrowIfFailedOnDebug().Succeeded)
				hr = pTaskbarList.Get()->HrInit().ThrowIfFailedOnDebug();
		}

		public HRESULT SetProgressValue(HWND hwnd, ulong ullCompleted, ulong ullTotal)
		{
			return pTaskbarList.Get()->SetProgressValue(hwnd, ullCompleted, ullTotal);
		}

		public HRESULT SetProgressState(HWND hwnd, TBPFLAG tbpFlags)
		{
			return pTaskbarList.Get()->SetProgressState(hwnd, tbpFlags);
		}

		public void Dispose()
		{
			pTaskbarList.Dispose();
		}
	}
}
