// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using WinRT.Interop;

namespace Files.App.Storage
{
	public sealed partial class WindowsBulkOperations : IDisposable
	{
		private unsafe partial struct WindowsBulkOperationsSink
		{
			private static readonly void** _lpPopulatedVtbl = PopulateVTable();

			private void** _lpVtbl;
			private volatile int _refCount;
			private GCHandle _operationsHandle;

			public static WindowsBulkOperationsSink* Create(WindowsBulkOperations operations)
			{
				WindowsBulkOperationsSink* operationsSink = (WindowsBulkOperationsSink*)NativeMemory.Alloc((nuint)sizeof(WindowsBulkOperationsSink));
				operationsSink->_lpVtbl = _lpPopulatedVtbl;
				operationsSink->_refCount = 1;
				operationsSink->_operationsHandle = GCHandle.Alloc(operations);

				return operationsSink;
			}

			private static void** PopulateVTable()
			{
				void** vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(WindowsBulkOperationsSink), sizeof(void*) * 19);
				vtbl[0] = (delegate* unmanaged<WindowsBulkOperationsSink*, Guid*, void**, HRESULT>)&Vtbl.QueryInterface;
				vtbl[1] = (delegate* unmanaged<WindowsBulkOperationsSink*, int>)&Vtbl.AddRef;
				vtbl[2] = (delegate* unmanaged<WindowsBulkOperationsSink*, int>)&Vtbl.Release;
				vtbl[3] = (delegate* unmanaged<WindowsBulkOperationsSink*, HRESULT>)&Vtbl.StartOperations;
				vtbl[4] = (delegate* unmanaged<WindowsBulkOperationsSink*, HRESULT, HRESULT>)&Vtbl.FinishOperations;
				vtbl[5] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, PCWSTR, HRESULT>)&Vtbl.PreRenameItem;
				vtbl[6] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, PCWSTR, HRESULT, IShellItem*, HRESULT>)&Vtbl.PostRenameItem;
				vtbl[7] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, IShellItem*, PCWSTR, HRESULT>)&Vtbl.PreMoveItem;
				vtbl[8] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, IShellItem*, PCWSTR, HRESULT, IShellItem*, HRESULT>)&Vtbl.PostMoveItem;
				vtbl[9] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, IShellItem*, PCWSTR, HRESULT>)&Vtbl.PreCopyItem;
				vtbl[10] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, IShellItem*, PCWSTR, HRESULT, IShellItem*, HRESULT>)&Vtbl.PostCopyItem;
				vtbl[11] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, HRESULT>)&Vtbl.PreDeleteItem;
				vtbl[12] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, HRESULT, IShellItem*, HRESULT>)&Vtbl.PostDeleteItem;
				vtbl[13] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, PCWSTR, HRESULT>)&Vtbl.PreNewItem;
				vtbl[14] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, IShellItem*, PCWSTR, PCWSTR, uint, HRESULT, IShellItem*, HRESULT>)&Vtbl.PostNewItem;
				vtbl[15] = (delegate* unmanaged<WindowsBulkOperationsSink*, uint, uint, HRESULT>)&Vtbl.UpdateProgress;
				vtbl[16] = (delegate* unmanaged<WindowsBulkOperationsSink*, HRESULT>)&Vtbl.ResetTimer;
				vtbl[17] = (delegate* unmanaged<WindowsBulkOperationsSink*, HRESULT>)&Vtbl.PauseTimer;
				vtbl[18] = (delegate* unmanaged<WindowsBulkOperationsSink*, HRESULT>)&Vtbl.ResumeTimer;

				return vtbl;
			}

			private static class Vtbl
			{
				[UnmanagedCallersOnly]
				public static HRESULT QueryInterface(WindowsBulkOperationsSink* @this, Guid* riid, void** ppv)
				{
					if (ppv is null)
						return HRESULT.E_POINTER;

					if (riid->Equals(IID.IID_IUnknown) || riid->Equals(IFileOperationProgressSink.IID_Guid))
					{
						Interlocked.Increment(ref @this->_refCount);
						*ppv = @this;
						return HRESULT.S_OK;
					}

					return HRESULT.E_NOINTERFACE;
				}

				[UnmanagedCallersOnly]
				public static int AddRef(WindowsBulkOperationsSink* @this)
					=> Interlocked.Increment(ref @this->_refCount);

				[UnmanagedCallersOnly]
				public static int Release(WindowsBulkOperationsSink* @this)
				{
					int newRefCount = Interlocked.Decrement(ref @this->_refCount);
					if (newRefCount is 0)
					{
						if (@this->_operationsHandle.IsAllocated)
							@this->_operationsHandle.Free();

						NativeMemory.Free(@this);
					}

					return newRefCount;
				}

				[UnmanagedCallersOnly]
				public static HRESULT StartOperations(WindowsBulkOperationsSink* @this)
					=> @this->StartOperations();

				[UnmanagedCallersOnly]
				public static HRESULT FinishOperations(WindowsBulkOperationsSink* @this, HRESULT hrResult)
					=> @this->FinishOperations(hrResult);

				[UnmanagedCallersOnly]
				public static HRESULT PreRenameItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiItem, PCWSTR pszNewName)
					=> @this->PreRenameItem(dwFlags, psiItem, pszNewName);

				[UnmanagedCallersOnly]
				public static HRESULT PostRenameItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiItem, PCWSTR pszNewName, HRESULT hrRename, IShellItem* psiNewlyCreated)
					=> @this->PostRenameItem(dwFlags, psiItem, pszNewName, hrRename, psiNewlyCreated);

				[UnmanagedCallersOnly]
				public static HRESULT PreMoveItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiItem, IShellItem* psiDestinationFolder, PCWSTR pszNewName)
					=> @this->PreMoveItem(dwFlags, psiItem, psiDestinationFolder, pszNewName);

				[UnmanagedCallersOnly]
				public static HRESULT PostMoveItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiItem, IShellItem* psiDestinationFolder, PCWSTR pszNewName, HRESULT hrMove, IShellItem* psiNewlyCreated)
					=> @this->PostMoveItem(dwFlags, psiItem, psiDestinationFolder, pszNewName, hrMove, psiNewlyCreated);

				[UnmanagedCallersOnly]
				public static HRESULT PreCopyItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiItem, IShellItem* psiDestinationFolder, PCWSTR pszNewName)
					=> @this->PreCopyItem(dwFlags, psiItem, psiDestinationFolder, pszNewName);

				[UnmanagedCallersOnly]
				public static HRESULT PostCopyItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiItem, IShellItem* psiDestinationFolder, PCWSTR pszNewName, HRESULT hrCopy, IShellItem* psiNewlyCreated)
					=> @this->PostCopyItem(dwFlags, psiItem, psiDestinationFolder, pszNewName, hrCopy, psiNewlyCreated);

				[UnmanagedCallersOnly]
				public static HRESULT PreDeleteItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiItem)
					=> @this->PreDeleteItem(dwFlags, psiItem);

				[UnmanagedCallersOnly]
				public static HRESULT PostDeleteItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiItem, HRESULT hrDelete, IShellItem* psiNewlyCreated)
					=> @this->PostDeleteItem(dwFlags, psiItem, hrDelete, psiNewlyCreated);

				[UnmanagedCallersOnly]
				public static HRESULT PreNewItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiDestinationFolder, PCWSTR pszNewName)
					=> @this->PreNewItem(dwFlags, psiDestinationFolder, pszNewName);

				[UnmanagedCallersOnly]
				public static HRESULT PostNewItem(WindowsBulkOperationsSink* @this, uint dwFlags, IShellItem* psiDestinationFolder, PCWSTR pszNewName, PCWSTR pszTemplateName, uint dwFileAttributes, HRESULT hrNew, IShellItem* psiNewItem)
					=> @this->PostNewItem(dwFlags, psiDestinationFolder, pszNewName, pszTemplateName, dwFileAttributes, hrNew, psiNewItem);

				[UnmanagedCallersOnly]
				public static HRESULT UpdateProgress(WindowsBulkOperationsSink* @this, uint iWorkTotal, uint iWorkSoFar)
					=> @this->UpdateProgress(iWorkTotal, iWorkSoFar);

				[UnmanagedCallersOnly]
				public static HRESULT ResetTimer(WindowsBulkOperationsSink* @this)
					=> @this->ResetTimer();

				[UnmanagedCallersOnly]
				public static HRESULT PauseTimer(WindowsBulkOperationsSink* @this)
					=> @this->PauseTimer();

				[UnmanagedCallersOnly]
				public static HRESULT ResumeTimer(WindowsBulkOperationsSink* @this)
					=> @this->ResumeTimer();
			}
		}
	}
}
