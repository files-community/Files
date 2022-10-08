using Microsoft.Windows.AppNotifications;
using System;

namespace Files.App.Helpers
{
    public class NotificationManager
    {
        private bool m_isRegistered;

        public NotificationManager()
        {
            m_isRegistered = false;
        }

        ~NotificationManager()
        {
            Unregister();
        }

        public void Init()
        {
            // To ensure all Notification handling happens in this process instance, register for
            // NotificationInvoked before calling Register(). Without this a new process will
            // be launched to handle the notification.
            AppNotificationManager notificationManager = AppNotificationManager.Default;

            notificationManager.NotificationInvoked += NotificationManager_NotificationInvoked;

            notificationManager.Register();
            m_isRegistered = true;
        }

        private void NotificationManager_NotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            // Todo: Complete later
        }

        public void Unregister()
        {
            if (m_isRegistered)
            {
                AppNotificationManager.Default.Unregister();
                m_isRegistered = false;
            }
        }

        public void ProcessLaunchActivationArgs(AppNotificationActivatedEventArgs notificationActivatedEventArgs)
        {
            throw new NotImplementedException();
            // Todo: Complete later
        }

    }
}
