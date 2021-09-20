// FilesOpenDialog.h: dichiarazione di CFilesOpenDialog

#pragma once
#include "resource.h"       // simboli principali



#include "CustomOpenDialog_i.h"
#include <iostream>
#include <string>
#include <vector>



#if defined(_WIN32_WCE) && !defined(_CE_DCOM) && !defined(_CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA)
#error "Gli oggetti COM a thread singolo non sono supportati correttamente sulla piattaforma Windows CE, ad esempio le piattaforme Windows Mobile non includono un supporto DCOM completo. Definire _CE_ALLOW_SINGLE_THREADED_OBJECTS_IN_MTA per fare in modo che ATL supporti la creazione di oggetti COM a thread singolo e consenta l'utilizzo di implementazioni con oggetti COM a thread singolo. Il modello di threading nel file RGS è stato impostato su 'Free' poiché è l'unico modello di threading supportato sulle piattaforme Windows CE non DCOM."
#endif

using namespace ATL;


// CFilesOpenDialog

class ATL_NO_VTABLE CFilesOpenDialog :
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CFilesOpenDialog, &CLSID_FilesOpenDialog>,
	public IFileDialog,
	public IFileDialog2,
	public IFileOpenDialog,
	public IFileDialogCustomize
{
public:
	CFilesOpenDialog();

DECLARE_REGISTRY_RESOURCEID(106)


#define DEBUGLOG

#ifdef  DEBUGLOG

#define CUSTOM_BEGIN_COM_MAP(x) public: \
	typedef x _ComMapClass; \
	static HRESULT WINAPI _Cache(_In_ void* pv, _In_ REFIID iid, _COM_Outptr_result_maybenull_ void** ppvObject, _In_ DWORD_PTR dw) throw()\
	{\
		_ComMapClass* p = (_ComMapClass*)pv;\
		p->Lock();\
		HRESULT hRes = E_FAIL; \
		__try \
		{ \
			hRes = ATL::CComObjectRootBase::_Cache(pv, iid, ppvObject, dw);\
		} \
		__finally \
		{ \
			p->Unlock();\
		} \
		return hRes;\
	}\
	IUnknown* _GetRawUnknown() throw() \
	{ ATLASSERT(_GetEntries()[0].pFunc == _ATL_SIMPLEMAPENTRY); return (IUnknown*)((INT_PTR)this+_GetEntries()->dw); } \
	_ATL_DECLARE_GET_UNKNOWN(x)\
	HRESULT _InternalQueryInterface( \
		_In_ REFIID iid, \
		_COM_Outptr_ void** ppvObject) throw() \
	{ \
		HRESULT res = this->InternalQueryInterface(this, _GetEntries(), iid, ppvObject); \
		OLECHAR* guidString; \
		(void)StringFromCLSID(iid, &guidString); \
		std::wcout << L"QueryInterface: " << guidString << L" = " << res << std::endl; \
		::CoTaskMemFree(guidString); \
		return res; \
	} \
	const static ATL::_ATL_INTMAP_ENTRY* WINAPI _GetEntries() throw() { \
	static const ATL::_ATL_INTMAP_ENTRY _entries[] = { DEBUG_QI_ENTRY(x)

#else

#define CUSTOM_BEGIN_COM_MAP(x) BEGIN_COM_MAP(x)

#endif //  DEBUGLOG

CUSTOM_BEGIN_COM_MAP(CFilesOpenDialog)
	COM_INTERFACE_ENTRY(IFileDialog)
	COM_INTERFACE_ENTRY(IFileDialog2)
	COM_INTERFACE_ENTRY(IFileOpenDialog)
	COM_INTERFACE_ENTRY(IFileDialogCustomize)
END_COM_MAP()

	DECLARE_PROTECT_FINAL_CONSTRUCT()

	HRESULT FinalConstruct()
	{
		return S_OK;
	}

	void FinalRelease();

	CComPtr<IFileOpenDialog> _systemDialog;

	FILEOPENDIALOGOPTIONS _fos;

	std::vector<std::wstring> _selectedItems;
	std::wstring _outputPath;
	IShellItem* _initFolder;
	IFileDialogEvents* _dialogEvents;

	FILE* _debugStream;

public:
	// Ereditato tramite IFileOpenDialog
	HRESULT __stdcall Show(HWND hwndOwner) override;

	HRESULT __stdcall SetFileTypes(UINT cFileTypes, const COMDLG_FILTERSPEC* rgFilterSpec) override;

	HRESULT __stdcall SetFileTypeIndex(UINT iFileType) override;

	HRESULT __stdcall GetFileTypeIndex(UINT* piFileType) override;

	HRESULT __stdcall Advise(IFileDialogEvents* pfde, DWORD* pdwCookie) override;

	HRESULT __stdcall Unadvise(DWORD dwCookie) override;

	HRESULT __stdcall SetOptions(FILEOPENDIALOGOPTIONS fos) override;

	HRESULT __stdcall GetOptions(FILEOPENDIALOGOPTIONS* pfos) override;

	HRESULT __stdcall SetDefaultFolder(IShellItem* psi) override;

	HRESULT __stdcall SetFolder(IShellItem* psi) override;

	HRESULT __stdcall GetFolder(IShellItem** ppsi) override;

	HRESULT __stdcall GetCurrentSelection(IShellItem** ppsi) override;

	HRESULT __stdcall SetFileName(LPCWSTR pszName) override;

	HRESULT __stdcall GetFileName(LPWSTR* pszName) override;

	HRESULT __stdcall SetTitle(LPCWSTR pszTitle) override;

	HRESULT __stdcall SetOkButtonLabel(LPCWSTR pszText) override;

	HRESULT __stdcall SetFileNameLabel(LPCWSTR pszLabel) override;

	HRESULT __stdcall GetResult(IShellItem** ppsi) override;

	HRESULT __stdcall AddPlace(IShellItem* psi, FDAP fdap) override;

	HRESULT __stdcall SetDefaultExtension(LPCWSTR pszDefaultExtension) override;

	HRESULT __stdcall Close(HRESULT hr) override;

	HRESULT __stdcall SetClientGuid(REFGUID guid) override;

	HRESULT __stdcall ClearClientData(void) override;

	HRESULT __stdcall SetFilter(IShellItemFilter* pFilter) override;

	HRESULT __stdcall GetResults(IShellItemArray** ppenum) override;

	HRESULT __stdcall GetSelectedItems(IShellItemArray** ppsai) override;


	// Ereditato tramite IFileDialogCustomize
	HRESULT __stdcall EnableOpenDropDown(DWORD dwIDCtl) override;

	HRESULT __stdcall AddMenu(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	HRESULT __stdcall AddPushButton(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	HRESULT __stdcall AddComboBox(DWORD dwIDCtl) override;

	HRESULT __stdcall AddRadioButtonList(DWORD dwIDCtl) override;

	HRESULT __stdcall AddCheckButton(DWORD dwIDCtl, LPCWSTR pszLabel, BOOL bChecked) override;

	HRESULT __stdcall AddEditBox(DWORD dwIDCtl, LPCWSTR pszText) override;

	HRESULT __stdcall AddSeparator(DWORD dwIDCtl) override;

	HRESULT __stdcall AddText(DWORD dwIDCtl, LPCWSTR pszText) override;

	HRESULT __stdcall SetControlLabel(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	HRESULT __stdcall GetControlState(DWORD dwIDCtl, CDCONTROLSTATEF* pdwState) override;

	HRESULT __stdcall SetControlState(DWORD dwIDCtl, CDCONTROLSTATEF dwState) override;

	HRESULT __stdcall GetEditBoxText(DWORD dwIDCtl, WCHAR** ppszText) override;

	HRESULT __stdcall SetEditBoxText(DWORD dwIDCtl, LPCWSTR pszText) override;

	HRESULT __stdcall GetCheckButtonState(DWORD dwIDCtl, BOOL* pbChecked) override;

	HRESULT __stdcall SetCheckButtonState(DWORD dwIDCtl, BOOL bChecked) override;

	HRESULT __stdcall AddControlItem(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel) override;

	HRESULT __stdcall RemoveControlItem(DWORD dwIDCtl, DWORD dwIDItem) override;

	HRESULT __stdcall RemoveAllControlItems(DWORD dwIDCtl) override;

	HRESULT __stdcall GetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF* pdwState) override;

	HRESULT __stdcall SetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF dwState) override;

	HRESULT __stdcall GetSelectedControlItem(DWORD dwIDCtl, DWORD* pdwIDItem) override;

	HRESULT __stdcall SetSelectedControlItem(DWORD dwIDCtl, DWORD dwIDItem) override;

	HRESULT __stdcall StartVisualGroup(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	HRESULT __stdcall EndVisualGroup(void) override;

	HRESULT __stdcall MakeProminent(DWORD dwIDCtl) override;

	HRESULT __stdcall SetControlItemText(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel) override;


	// Ereditato tramite IFileDialog2
	HRESULT __stdcall SetCancelButtonLabel(LPCWSTR pszLabel) override;

	HRESULT __stdcall SetNavigationRoot(IShellItem* psi) override;
};

OBJECT_ENTRY_AUTO(__uuidof(FilesOpenDialog), CFilesOpenDialog)
