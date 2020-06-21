#include "pch.h"
#include "Wnf.h"
#include "TEBProc.h"

RtlSubscribeWnfStateChangeNotification pRtlSubscribeWnfStateChangeNotification;
RtlUnsubscribeWnfStateChangeNotification pRtlUnsubscribeWnfStateChangeNotification;
NtQueryWnfStateData pNtQueryWnfStateData;

void Wnf::InitLib()
{
	pRtlSubscribeWnfStateChangeNotification = (RtlSubscribeWnfStateChangeNotification)GetProcAddressNew(L"ntdll.dll", L"RtlSubscribeWnfStateChangeNotification");
	pRtlUnsubscribeWnfStateChangeNotification = (RtlUnsubscribeWnfStateChangeNotification)GetProcAddressNew(L"ntdll.dll", L"RtlUnsubscribeWnfStateChangeNotification");
	pNtQueryWnfStateData = (NtQueryWnfStateData)GetProcAddressNew(L"ntdll.dll", L"NtQueryWnfStateData");
}

void Wnf::SubscribeWnf(ULONG64 state_name, decltype(WnfCallback)* callback, intptr_t callback_param)
{
	uint32_t buf1{};
	size_t buf2{};

	NTSTATUS result = pRtlSubscribeWnfStateChangeNotification(&buf2, state_name, buf1, *callback, callback_param, 0, 0, 1);
}

void Wnf::UnsubscribeWnf(decltype(WnfCallback)* callback)
{
	NTSTATUS result = pRtlUnsubscribeWnfStateChangeNotification(callback);
}

bool Wnf::IsTabletMode()
{
	auto vector = QueryWnf(WNF_TMCN_ISTABLETMODE);
	return ToBool(vector.data());
}

std::vector<unsigned char> Wnf::QueryWnf(ULONG64 state_name)
{
	std::vector<unsigned char> wnf_state_buffer(8192);
	unsigned long state_buffer_size = 8192;
	WNF_CHANGE_STAMP wnf_change_stamp = { 0 };

	pNtQueryWnfStateData(&state_name, nullptr, nullptr, &wnf_change_stamp,
		wnf_state_buffer.data(), &state_buffer_size);

	return wnf_state_buffer;
}