#pragma once

/*
MIT License
Copyright (c) 2019 Gustave Monce - @gus33000 - gus33000.me
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
Re-adapted from: https://raw.githubusercontent.com/gus33000/RegistryRT/master/pch.h
*/

#pragma once
#include "pch.h"
#include <Windows.h>
#include <winnt.h>
#include <ppltasks.h>
#include <winrt/base.h>
#include <algorithm>

using namespace winrt;

typedef struct _UNICODE_STRING {
	USHORT Length;
	USHORT MaximumLength;
	PWSTR  Buffer;
} UNICODE_STRING;

typedef struct _PEB_LDR_DATA {
	BYTE Reserved1[8];
	PVOID Reserved2[3];
	LIST_ENTRY InMemoryOrderModuleList;
} PEB_LDR_DATA, * PPEB_LDR_DATA;

typedef struct _RTL_USER_PROCESS_PARAMETERS {
	BYTE Reserved1[16];
	PVOID Reserved2[10];
	UNICODE_STRING ImagePathName;
	UNICODE_STRING CommandLine;
} RTL_USER_PROCESS_PARAMETERS, * PRTL_USER_PROCESS_PARAMETERS;

typedef struct _PEB {
	BYTE Reserved1[2];
	BYTE BeingDebugged;
	BYTE Reserved2[1];
	PVOID Reserved3[2];
	PPEB_LDR_DATA Ldr;
	PRTL_USER_PROCESS_PARAMETERS ProcessParameters;
	PVOID Reserved4[3];
	PVOID AtlThunkSListPtr;
	PVOID Reserved5;
	ULONG Reserved6;
	PVOID Reserved7;
	ULONG Reserved8;
	ULONG AtlThunkSListPtr32;
	PVOID Reserved9[45];
	BYTE Reserved10[96];
	PVOID PostProcessInitRoutine;
	BYTE Reserved11[128];
	PVOID Reserved12[1];
	ULONG SessionId;
} PEB, * PPEB;

typedef struct _MY_PEB_LDR_DATA {
	ULONG Length;
	BOOL Initialized;
	PVOID SsHandle;
	LIST_ENTRY InLoadOrderModuleList;
	LIST_ENTRY InMemoryOrderModuleList;
	LIST_ENTRY InInitializationOrderModuleList;
} MY_PEB_LDR_DATA, * PMY_PEB_LDR_DATA;

typedef struct _MY_LDR_DATA_TABLE_ENTRY
{
	LIST_ENTRY InLoadOrderLinks;
	LIST_ENTRY InMemoryOrderLinks;
	LIST_ENTRY InInitializationOrderLinks;
	PVOID DllBase;
	PVOID EntryPoint;
	ULONG SizeOfImage;
	UNICODE_STRING FullDllName;
	UNICODE_STRING BaseDllName;
} MY_LDR_DATA_TABLE_ENTRY, * PMY_LDR_DATA_TABLE_ENTRY;

inline winrt::hstring Str(UNICODE_STRING US) {
	wchar_t* str = (wchar_t*)malloc(US.Length + sizeof(wchar_t));
	memcpy(str, US.Buffer, US.Length);
	str[US.Length / sizeof(wchar_t)] = 0;
	winrt::hstring ret(str);
	free(str);
	return ret;
}

inline winrt::hstring Str(const char* char_array) {
	std::string s_str = std::string(char_array);
	std::wstring wid_str = std::wstring(s_str.begin(), s_str.end());
	const wchar_t* w_char = wid_str.c_str();
	return winrt::hstring(w_char);
}

inline winrt::hstring ToLower(winrt::hstring str) {
	std::wstring wid_str = str.data();
	std::transform(wid_str.begin(), wid_str.end(), wid_str.begin(), ::tolower);
	return winrt::hstring(wid_str.c_str());
}

inline PEB* NtCurrentPeb() {
#ifdef _M_X64
	return (PEB*)(__readgsqword(0x60));
#elif _M_IX86
	return (PEB*)(__readfsdword(0x30));
#elif _M_ARM
	return *(PEB**)(_MoveFromCoprocessor(15, 0, 13, 0, 2) + 0x30);
#elif _M_ARM64
	return *(PEB**)(__getReg(18) + 0x60); // TEB in x18
#elif _M_IA64
	return *(PEB**)(_rdteb() + 0x60);
#elif _M_ALPHA
	return *(PEB**)(_rdteb() + 0x30);
#elif _M_MIPS
	return *(PEB**)((*(char**)(0x7ffff030)) + 0x30);
#elif _M_PPC
	// winnt.h of the period uses __builtin_get_gpr13() or __gregister_get(13) depending on _MSC_VER
	return *(PEB**)(__gregister_get(13) + 0x30);
#else
#error "This architecture is currently unsupported"
#endif
};

#define IMAGE_DIRECTORY_ENTRY_EXPORT 0

inline FARPROC GetProcAddressNew(winrt::hstring dll, winrt::hstring func) {
	dll = ToLower(dll);
	func = ToLower(func);
	auto Ldr = (PMY_PEB_LDR_DATA)(NtCurrentPeb()->Ldr);
	auto NextModule = Ldr->InLoadOrderModuleList.Flink;
	auto TableEntry = (PMY_LDR_DATA_TABLE_ENTRY)NextModule;
	while (TableEntry->DllBase != NULL) {
		PVOID base = TableEntry->DllBase;
		winrt::hstring dllName = ToLower(Str(TableEntry->BaseDllName));
		auto PE = (PIMAGE_NT_HEADERS)((ULONG_PTR)base + ((PIMAGE_DOS_HEADER)base)->e_lfanew);
		auto exportdirRVA = PE->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress;
		TableEntry = (PMY_LDR_DATA_TABLE_ENTRY)TableEntry->InLoadOrderLinks.Flink;

		if (exportdirRVA == 0) continue;
		if (dllName != dll) continue;

		auto Exports = (PIMAGE_EXPORT_DIRECTORY)((ULONG_PTR)base + exportdirRVA);
		auto Names = (PDWORD)((PCHAR)base + Exports->AddressOfNames);
		auto Ordinals = (PUSHORT)((ULONG_PTR)base + Exports->AddressOfNameOrdinals);
		auto Functions = (PDWORD)((ULONG_PTR)base + Exports->AddressOfFunctions);

		for (UINT32 iterator = 0; iterator < Exports->NumberOfNames; iterator++) {
			winrt::hstring funcName = ToLower(Str((PCSTR)(Names[iterator] + (ULONG_PTR)base)));

			if (funcName != func) continue;

			USHORT ordTblIndex = Ordinals[iterator];
			return (FARPROC)((ULONG_PTR)base + Functions[ordTblIndex]);
		}
	}
	return NULL;
}