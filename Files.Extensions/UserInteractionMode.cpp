#include "pch.h"
#include "UserInteractionMode.h"
#include "UserInteractionMode.g.cpp"
#include <windows.h>

using namespace winrt;
using namespace winrt::Windows;

namespace winrt::Files_Extensions::implementation
{
	Windows::UI::Xaml::DispatcherTimer m_timer;
	int currentSlateState = 0;

	NTSTATUS NTAPI WnfCallback(const ULONG64 state_name, void* p2, void* p3, void* callbackContext, void* buffer, ULONG bufferSize)
	{
		if (((UserInteractionMode*)callbackContext)->IsSubscribed())
			((UserInteractionMode*)callbackContext)->InvokeTabletModeChange();

		return 0;
	}

	void UserInteractionMode::InvokeTabletModeChange()
	{
		const auto inter = GetUserInteractionMode();
		event_userInteractionModeChanged(*this, inter);
	}

	event_token UserInteractionMode::UserInteractionModeChanged(Windows::Foundation::EventHandler<UserInteractionType> const& handler)
	{
		if (numSub_userInteractionModeChangedEvent++ == 0)
		{
			isSubscribed = true;
			currentSlateState = GetSlateState();

			Windows::Foundation::TimeSpan dur(2500000L);
			m_timer.Interval(dur);
			m_timer.Tick([this](Windows::Foundation::IInspectable const&, Windows::Foundation::IInspectable const&)
				{
					const auto curSlat = GetSlateState();
					if (curSlat != currentSlateState)
					{
						currentSlateState = curSlat;
						InvokeTabletModeChange();
					}
					else
						currentSlateState = curSlat;
				});
			m_timer.Start();

			if (firstTimeSubscription)
			{
				Wnf::SubscribeWnf(WNF_TMCN_ISTABLETMODE, WnfCallback, (intptr_t)this);
				firstTimeSubscription = false;
			}
		}

		return event_userInteractionModeChanged.add(handler);
	}

	void UserInteractionMode::UserInteractionModeChanged(winrt::event_token const& token) noexcept
	{
		if (--numSub_userInteractionModeChangedEvent == 0)
		{
			//Wnf::UnsubscribeWnf(WnfCallback);
			isSubscribed = false;
			m_timer.Stop();
		}

		event_userInteractionModeChanged.remove(token);
	}

	UserInteractionType UserInteractionMode::GetUserInteractionMode()
	{
		bool res = Wnf::IsTabletMode();
		bool slate = GetSlateState();

		if (res)
		{
			if (slate)
				return UserInteractionType::TouchInTabletMode;
			else
				return UserInteractionType::MouseInTabletMode;
		}
		else
		{
			if (slate)
				return UserInteractionType::TouchInDesktopMode;
			else
				return UserInteractionType::MouseInDesktopMode;
		}
	}

	//0 = normal mode, 1 = slate mode, -1 = not a slate
	int UserInteractionMode::GetSlateStateInt()
	{
		const auto isSlate = SysProc::GetSlateState();
		if (isSlate)
			return 0; //it is not
		else
		{
			const auto isTabletPC = SysProc::GetTabletPCState();
			if (isTabletPC)
				return 1;
			else
				return -1;
		}
	}

	//0 = normal mode, 1 = slate mode
	int UserInteractionMode::GetSlateState()
	{
		const auto ver = GetSlateStateInt();

		if (ver == -1)
			return 0;

		return ver;
	}

	int UserInteractionMode::GetDockState()
	{
		return SysProc::GetDockState();
	}
}