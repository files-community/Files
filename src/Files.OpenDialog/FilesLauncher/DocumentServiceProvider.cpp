// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

#include "DocumentServiceProvider.h"

// IDispatch
// Note:
//  None of these methods require any implementation. When attempting to find a shell window,
//  the shell requests the IDispatch interface, but doesn't actually call any methods on it.
IFACEMETHODIMP DocumentServiceProvider::GetTypeInfoCount(UINT *typeInfoCount)
{
	UNREFERENCED_PARAMETER(typeInfoCount);

	return E_NOTIMPL;
}

IFACEMETHODIMP DocumentServiceProvider::GetTypeInfo(UINT type, LCID localeId, ITypeInfo **typeInfo)
{
	UNREFERENCED_PARAMETER(type);
	UNREFERENCED_PARAMETER(localeId);
	UNREFERENCED_PARAMETER(typeInfo);

	return E_NOTIMPL;
}

IFACEMETHODIMP DocumentServiceProvider::GetIDsOfNames(REFIID riid, LPOLESTR *names, UINT numNames, LCID localeId, DISPID *dispId)
{
	UNREFERENCED_PARAMETER(riid);
	UNREFERENCED_PARAMETER(names);
	UNREFERENCED_PARAMETER(numNames);
	UNREFERENCED_PARAMETER(localeId);
	UNREFERENCED_PARAMETER(dispId);

	return E_NOTIMPL;
}

IFACEMETHODIMP DocumentServiceProvider::Invoke(DISPID dispIdMember, REFIID riid, LCID localeId, WORD flags, DISPPARAMS *dispParams, VARIANT *varResult, EXCEPINFO *exceptionInfo, UINT *argErr)
{
	UNREFERENCED_PARAMETER(dispIdMember);
	UNREFERENCED_PARAMETER(riid);
	UNREFERENCED_PARAMETER(localeId);
	UNREFERENCED_PARAMETER(flags);
	UNREFERENCED_PARAMETER(dispParams);
	UNREFERENCED_PARAMETER(varResult);
	UNREFERENCED_PARAMETER(exceptionInfo);
	UNREFERENCED_PARAMETER(argErr);

	return E_NOTIMPL;
}

// IServiceProvider
IFACEMETHODIMP DocumentServiceProvider::QueryService(REFGUID guidService, REFIID riid, void **ppv)
{
	return ServiceProviderBase::QueryService(guidService, riid, ppv);
}
