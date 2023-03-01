// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

#include "ServiceProviderBase.h"

void ServiceProviderBase::RegisterService(REFGUID guidService, IUnknown *service)
{
	m_services[guidService] = service;
}

HRESULT ServiceProviderBase::QueryService(REFGUID guidService, REFIID riid, void **ppv)
{
	auto itr = m_services.find(guidService);

	if (itr == m_services.end())
	{
		return E_NOINTERFACE;
	}

	return itr->second->QueryInterface(riid, ppv);
	return S_OK;
}
