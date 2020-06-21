#include "pch.h"
#include "SysProc.h"

GetSystemMetrics pGetSystemMetrics;

void SysProc::InitLib()
{
	pGetSystemMetrics = (GetSystemMetrics)GetProcAddressNew(L"user32.dll", L"GetSystemMetrics");
}

int SysProc::GetDockState()
{
	return pGetSystemMetrics(SM_SYSTEMDOCKED);
}

int SysProc::GetSlateState()
{
	return pGetSystemMetrics(SM_CONVERTIBLESLATEMODE);
}

int SysProc::GetTabletPCState()
{
	return pGetSystemMetrics(SM_TABLETPC);
}