using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace Files
{
    public sealed partial class UnhandledExceptionDisplay : Page
    {
        public UnhandledExceptionDisplay()
        {
            this.InitializeComponent();
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            CoreTitleBar.ExtendViewIntoTitleBar = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            var Parameter = eventArgs.Parameter as Exception;
            Summary.Text = Parameter.Message + " within " + Parameter.TargetSite;
            ErrorInfo.Text = Parameter.StackTrace;
        }

        // Restart app
        private async void Restart_Click(object sender, RoutedEventArgs e)
        {
            var result = await CoreApplication.RequestRestartAsync("Application Restart Programmatically ");  
            if (result == AppRestartFailureReason.NotInForeground ||  
                result == AppRestartFailureReason.RestartPending ||  
                result == AppRestartFailureReason.Other)  
            {
				// The old method of starting a new instance and exiting the current one
				await Launcher.LaunchUriAsync(new Uri(@"files-uwp:///"));
				CoreApplication.Exit();
            }
        }

        // Report issue
        private async void Report_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/files-uwp/issues/new/choose"));
        }
		
		private async void OpenLogLocation_Click(object sender, RoutedEventArgs e)
		{
			await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
		}
    }
}