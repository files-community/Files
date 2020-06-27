#pragma once
#include "UserInteractionMode.g.h"
#include <winstring.h>
#include "Wnf.h"
#include "SysProc.h"
#include "winrt/Windows.System.Profile.h"
#include "winrt/Windows.UI.Xaml.h"
#include "winrt/Windows.Foundation.h"

namespace winrt::Files_Extensions::implementation
{
	struct UserInteractionMode : UserInteractionModeT<UserInteractionMode>
	{
		UserInteractionMode()
		{
			Wnf::InitLib();
			SysProc::InitLib();
		}

		winrt::event_token UserInteractionModeChanged(Windows::Foundation::EventHandler<UserInteractionType> const& handler);
		void UserInteractionModeChanged(winrt::event_token const& token) noexcept;

		UserInteractionType GetUserInteractionMode();
		int GetSlateState();
		int GetDockState();

		void InvokeTabletModeChange();
		bool IsSubscribed()
		{
			return isSubscribed;
		}
	private:
		int GetSlateStateInt();
		winrt::event<Windows::Foundation::EventHandler<UserInteractionType>> event_userInteractionModeChanged;
		int numSub_userInteractionModeChangedEvent = 0;
		bool firstTimeSubscription = true;
		bool isSubscribed = false;
	};
}
namespace winrt::Files_Extensions::factory_implementation
{
	struct UserInteractionMode : UserInteractionModeT<UserInteractionMode, implementation::UserInteractionMode>
	{
	};
}
