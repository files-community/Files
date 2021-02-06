#include "pch.h"
#include "MsgHandler_BitLocker.h"
#include <string>
#include <sstream>

bool MsgHandler_BitLocker::Unlock(LPCWSTR filepath, LPCWSTR password)
{
	using namespace::std;

	wstringstream strStream;
	strStream
		<< L"-command \"$SecureString = ConvertTo-SecureString '"
		<< *password
		<< L"' -AsPlainText -Force; Unlock-BitLocker - MountPoint '"
		<< *filepath
		<< L"' -Password $SecureString\"";

	wstring args = strStream.str();

	(void)ShellExecute(NULL, L"runas", L"powershell.exe", args.c_str(), NULL, FALSE);

	return true;
}

bool MsgHandler_BitLocker::Lock(LPCWSTR filepath, LPCWSTR password)
{
	using namespace::std;

	wstringstream strStream;
	strStream
		<< L"-command \"$SecureString = ConvertTo-SecureString '"
		<< *password
		<< L"' -AsPlainText -Force; Lock-BitLocker - MountPoint '"
		<< *filepath
		<< L"' -Password $SecureString\"";

	wstring args = strStream.str();

	(void)ShellExecute(NULL, L"runas", L"powershell.exe", args.c_str(), NULL, FALSE);

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
			hstring filepath = args.Request().Message().Lookup(L"FilePath").as<hstring>();
			hstring password = args.Request().Message().Lookup(L"Password").as<hstring>();

			if (action == L"Unlock")
			{
				if (Unlock(filepath.c_str(), password.c_str()))
				{
					ValueSet response;
					response.Insert(L"Bitlocker", winrt::box_value(L"Success"));

					co_await args.Request().SendResponseAsync(response);
				}
				else
				{
					ValueSet response;
					response.Insert(L"Bitlocker", winrt::box_value(L"Failed"));

					co_await args.Request().SendResponseAsync(response);
				}

				co_return TRUE;
			}
			else if (action == L"Lock")
			{
				if (Lock(filepath.c_str(), password.c_str()))
				{
					ValueSet response;
					response.Insert(L"Bitlocker", winrt::box_value(L"Success"));

					co_await args.Request().SendResponseAsync(response);
				}
				else
				{
					ValueSet response;
					response.Insert(L"Bitlocker", winrt::box_value(L"Failed"));

					co_await args.Request().SendResponseAsync(response);
				}

				co_return TRUE;
			}
		}
	}

	co_return FALSE;
}