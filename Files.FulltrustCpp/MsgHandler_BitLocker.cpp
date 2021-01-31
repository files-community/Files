#include "pch.h"
#include "MsgHandler_BitLocker.h"
#include <string>

bool MsgHandler_BitLocker::Unlock(LPCWSTR filepath, LPCWSTR password)
{
	using namespace::std;

	wstring args = L"-command \"$SecureString = ConvertTo-SecureString '";
	args += *password;
	args += L"' -AsPlainText -Force; Unlock-BitLocker - MountPoint '";
	args += *filepath;
	args += L"' -Password $SecureString\"";

	auto hInstance = ShellExecute(NULL, L"runas", L"powershell.exe", args.c_str(), NULL, 0);

	return true;
}

bool MsgHandler_BitLocker::Lock(LPCWSTR filepath, LPCWSTR password)
{
	using namespace::std;

	wstring args = L"-command \"$SecureString = ConvertTo-SecureString '";
	args += *password;
	args += L"' -AsPlainText -Force; Lock-BitLocker - MountPoint '";
	args += *filepath;
	args += L"' -Password $SecureString\"";

	auto hInstance = ShellExecute(NULL, L"runas", L"powershell.exe", args.c_str(), NULL, 0);

	return true;
}

IAsyncOperation<bool> MsgHandler_BitLocker::ParseArgumentsAsync(const AppServiceManager& manager, const AppServiceRequestReceivedEventArgs& args)
{
	using namespace::winrt;

	if (args.Request().Message().HasKey(L"Arguments"))
	{
		hstring arguments = args.Request().Message().Lookup(L"Arguments").as<hstring>();

		if (arguments == L"Bitlocker")
		{
			hstring action = args.Request().Message().Lookup(L"Action").as<hstring>();
			hstring filepath = args.Request().Message().Lookup(L"filepath").as<hstring>();
			hstring password = args.Request().Message().Lookup(L"password").as<hstring>();

			if (action == L"Unlock")
			{
				if (Unlock(filepath.c_str(), password.c_str()))
				{
					ValueSet response;
					response.Insert(L"Bitlocker", winrt::box_value("Success"));

					co_await args.Request().SendResponseAsync(response);
				}
				else
				{
					ValueSet response;
					response.Insert(L"Bitlocker", winrt::box_value("Failed"));

					co_await args.Request().SendResponseAsync(response);
				}

				co_return TRUE;
			}
			else if (action == L"Lock")
			{
				if (Lock(filepath.c_str(), password.c_str()))
				{
					ValueSet response;
					response.Insert(L"Bitlocker", winrt::box_value("Success"));

					co_await args.Request().SendResponseAsync(response);
				}
				else
				{
					ValueSet response;
					response.Insert(L"Bitlocker", winrt::box_value("Failed"));

					co_await args.Request().SendResponseAsync(response);
				}

				co_return TRUE;
			}
		}
	}

	co_return FALSE;
}