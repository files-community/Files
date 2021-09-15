// FilesOpenDialog.h: dichiarazione di CFilesOpenDialog

#pragma once
#include "resource.h"       // simboli principali



#include "CustomOpenDialog_i.h"
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


BEGIN_COM_MAP(CFilesOpenDialog)
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

	virtual void FinalRelease();

	CComPtr<IFileOpenDialog> _systemDialog;

	FILEOPENDIALOGOPTIONS _fos;

	std::vector<std::wstring> _selectedItems;
	std::wstring _outputPath;
	IShellItem* _initFolder;
	IFileDialogEvents* _dialogEvents;

	FILE* _debugStream;

public:

	// Ereditato tramite IFileOpenDialog
	virtual HRESULT __stdcall Show(HWND hwndOwner) override;

	virtual HRESULT __stdcall SetFileTypes(UINT cFileTypes, const COMDLG_FILTERSPEC* rgFilterSpec) override;

	virtual HRESULT __stdcall SetFileTypeIndex(UINT iFileType) override;

	virtual HRESULT __stdcall GetFileTypeIndex(UINT* piFileType) override;

	virtual HRESULT __stdcall Advise(IFileDialogEvents* pfde, DWORD* pdwCookie) override;

	virtual HRESULT __stdcall Unadvise(DWORD dwCookie) override;

	virtual HRESULT __stdcall SetOptions(FILEOPENDIALOGOPTIONS fos) override;

	virtual HRESULT __stdcall GetOptions(FILEOPENDIALOGOPTIONS* pfos) override;

	virtual HRESULT __stdcall SetDefaultFolder(IShellItem* psi) override;

	virtual HRESULT __stdcall SetFolder(IShellItem* psi) override;

	virtual HRESULT __stdcall GetFolder(IShellItem** ppsi) override;

	virtual HRESULT __stdcall GetCurrentSelection(IShellItem** ppsi) override;

	virtual HRESULT __stdcall SetFileName(LPCWSTR pszName) override;

	virtual HRESULT __stdcall GetFileName(LPWSTR* pszName) override;

	virtual HRESULT __stdcall SetTitle(LPCWSTR pszTitle) override;

	virtual HRESULT __stdcall SetOkButtonLabel(LPCWSTR pszText) override;

	virtual HRESULT __stdcall SetFileNameLabel(LPCWSTR pszLabel) override;

	virtual HRESULT __stdcall GetResult(IShellItem** ppsi) override;

	virtual HRESULT __stdcall AddPlace(IShellItem* psi, FDAP fdap) override;

	virtual HRESULT __stdcall SetDefaultExtension(LPCWSTR pszDefaultExtension) override;

	virtual HRESULT __stdcall Close(HRESULT hr) override;

	virtual HRESULT __stdcall SetClientGuid(REFGUID guid) override;

	virtual HRESULT __stdcall ClearClientData(void) override;

	virtual HRESULT __stdcall SetFilter(IShellItemFilter* pFilter) override;

	virtual HRESULT __stdcall GetResults(IShellItemArray** ppenum) override;

	virtual HRESULT __stdcall GetSelectedItems(IShellItemArray** ppsai) override;


	// Ereditato tramite IFileDialogCustomize
	virtual HRESULT __stdcall EnableOpenDropDown(DWORD dwIDCtl) override;

	virtual HRESULT __stdcall AddMenu(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	virtual HRESULT __stdcall AddPushButton(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	virtual HRESULT __stdcall AddComboBox(DWORD dwIDCtl) override;

	virtual HRESULT __stdcall AddRadioButtonList(DWORD dwIDCtl) override;

	virtual HRESULT __stdcall AddCheckButton(DWORD dwIDCtl, LPCWSTR pszLabel, BOOL bChecked) override;

	virtual HRESULT __stdcall AddEditBox(DWORD dwIDCtl, LPCWSTR pszText) override;

	virtual HRESULT __stdcall AddSeparator(DWORD dwIDCtl) override;

	virtual HRESULT __stdcall AddText(DWORD dwIDCtl, LPCWSTR pszText) override;

	virtual HRESULT __stdcall SetControlLabel(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	virtual HRESULT __stdcall GetControlState(DWORD dwIDCtl, CDCONTROLSTATEF* pdwState) override;

	virtual HRESULT __stdcall SetControlState(DWORD dwIDCtl, CDCONTROLSTATEF dwState) override;

	virtual HRESULT __stdcall GetEditBoxText(DWORD dwIDCtl, WCHAR** ppszText) override;

	virtual HRESULT __stdcall SetEditBoxText(DWORD dwIDCtl, LPCWSTR pszText) override;

	virtual HRESULT __stdcall GetCheckButtonState(DWORD dwIDCtl, BOOL* pbChecked) override;

	virtual HRESULT __stdcall SetCheckButtonState(DWORD dwIDCtl, BOOL bChecked) override;

	virtual HRESULT __stdcall AddControlItem(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel) override;

	virtual HRESULT __stdcall RemoveControlItem(DWORD dwIDCtl, DWORD dwIDItem) override;

	virtual HRESULT __stdcall RemoveAllControlItems(DWORD dwIDCtl) override;

	virtual HRESULT __stdcall GetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF* pdwState) override;

	virtual HRESULT __stdcall SetControlItemState(DWORD dwIDCtl, DWORD dwIDItem, CDCONTROLSTATEF dwState) override;

	virtual HRESULT __stdcall GetSelectedControlItem(DWORD dwIDCtl, DWORD* pdwIDItem) override;

	virtual HRESULT __stdcall SetSelectedControlItem(DWORD dwIDCtl, DWORD dwIDItem) override;

	virtual HRESULT __stdcall StartVisualGroup(DWORD dwIDCtl, LPCWSTR pszLabel) override;

	virtual HRESULT __stdcall EndVisualGroup(void) override;

	virtual HRESULT __stdcall MakeProminent(DWORD dwIDCtl) override;

	virtual HRESULT __stdcall SetControlItemText(DWORD dwIDCtl, DWORD dwIDItem, LPCWSTR pszLabel) override;


	// Ereditato tramite IFileDialog2
	virtual HRESULT __stdcall SetCancelButtonLabel(LPCWSTR pszLabel) override;

	virtual HRESULT __stdcall SetNavigationRoot(IShellItem* psi) override;

};

OBJECT_ENTRY_AUTO(__uuidof(FilesOpenDialog), CFilesOpenDialog)
