// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

#pragma once

#include "ServiceProviderBase.h"
#include <winrt/base.h>

class DocumentServiceProvider :
	public winrt::implements<DocumentServiceProvider, IDispatch, IServiceProvider>,
	public ServiceProviderBase
{
public:
	// IDispatch
	IFACEMETHODIMP GetTypeInfoCount(UINT *typeInfoCount);
	IFACEMETHODIMP GetTypeInfo(UINT type, LCID localeId, ITypeInfo **typeInfo);
	IFACEMETHODIMP GetIDsOfNames(
		REFIID riid, LPOLESTR *names, UINT numNames, LCID localeId, DISPID *dispId);
	IFACEMETHODIMP Invoke(DISPID dispIdMember, REFIID riid, LCID localeId, WORD flags,
		DISPPARAMS *dispParams, VARIANT *varResult, EXCEPINFO *exceptionInfo, UINT *argErr);

	// IServiceProvider
	IFACEMETHODIMP QueryService(REFGUID guidService, REFIID riid, void **ppv);
};
