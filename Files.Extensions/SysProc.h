#pragma once
#include "TEBProc.h"

typedef int (WINAPI* GetSystemMetrics)(
	_In_ int nIndex);

class SysProc
{
public:
	static void InitLib();
	static int GetDockState();
	static int GetSlateState();
	static int GetTabletPCState();
};
