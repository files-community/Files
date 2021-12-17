// dllmain.h: dichiarazione della classe del modulo.

class CCustomSaveDialogModule : public ATL::CAtlDllModuleT< CCustomSaveDialogModule >
{
public :
	DECLARE_LIBID(LIBID_CustomSaveDialogLib)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_CUSTOMSAVEDIALOG, "{21533617-c1cd-4d33-a190-21fb069b55f4}")
};

extern class CCustomSaveDialogModule _AtlModule;
