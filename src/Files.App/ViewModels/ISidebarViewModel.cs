using Files.App.Extensions;
using Files.App.Filesystem;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Files.App.ViewModels
{
	public interface ISidebarViewModel : INotifyPropertyChanged, IDisposable
	{
		IPaneHolder PaneHolder { get; set; }

		ICommand EmptyRecycleBinCommand { get; }

		NavigationViewDisplayMode SidebarDisplayMode { get; set; }
		INavigationControlItem? SidebarSelectedItem { get; set; }
		ICollection<INavigationControlItem> SideBarItems { get; }

		bool IsSidebarCompactSize { get; }
		bool IsSidebarOpen { get; set; }
		bool ShowFavoritesSection { get; set; }
		bool ShowLibrarySection { get; set; }
		bool ShowDrivesSection { get; set; }
		bool ShowCloudDrivesSection { get; set; }
		bool ShowNetworkDrivesSection { get; set; }
		bool ShowWslSection { get; set; }
		bool ShowFileTagsSection { get; set; }

		void UpdateSidebarSelectedItemFromArgs(string arg);
		void NotifyInstanceRelatedPropertiesChanged(string arg);

		void SidebarControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs e);
	}
}
