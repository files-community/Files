// Copyright (c) 2023 Files
// Licensed under the MIT License. See the LICENSE.

#pragma once

#include <wil/com.h>
#include <unordered_map>

namespace std {
	template<> struct hash<GUID>
	{
		size_t operator()(const GUID& guid) const noexcept {
			const std::uint64_t* p = reinterpret_cast<const std::uint64_t*>(&guid);
			std::hash<std::uint64_t> hash;
			return hash(p[0]) ^ hash(p[1]);
		}
	};
}

class ServiceProviderBase
{
public:
	void RegisterService(REFGUID guidService, IUnknown* service);
	HRESULT QueryService(REFGUID guidService, REFIID riid, void** ppv);

protected:
	ServiceProviderBase() = default;

private:
	std::unordered_map<IID, wil::com_ptr_nothrow<IUnknown>> m_services;
};