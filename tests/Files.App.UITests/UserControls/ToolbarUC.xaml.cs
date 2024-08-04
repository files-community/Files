using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;



namespace Files.App.UITests.UserControls
{
	public sealed partial class ToolbarUC : UserControl
	{
		public ObservableCollection<ToolbarItem> ToolbarItems;

		public ToolbarUC()
		{
			this.InitializeComponent();
		}



		private void InitializeData()
		{
			if ( ToolbarItems == null )
			{
				ToolbarItems = new ObservableCollection<ToolbarItem>();
			}
			ToolbarItems.Add( new ToolbarItem() { Label = "Test Label 1" , IconSize = 16 , ThemedIcon = (Style)Application.Current.Resources["App.ThemedIcons.CopyAsPath"] } );
			ToolbarItems.Add( new ToolbarItem() { Label = "Test Label 2" , IconSize = 16 , ThemedIcon = (Style)Application.Current.Resources["App.ThemedIcons.PasteShortcut"] } );
			ToolbarItems.Add( new ToolbarItem() { Label = "Test Label 3" , IconSize = 16 , ThemedIcon = (Style)Application.Current.Resources["App.ThemedIcons.MoveTo"] } );
		}



		private void UserControl_Loaded(object sender , RoutedEventArgs e)
		{
			InitializeData();

			if ( testToolbar != null )
			{
				//testToolbar.Items = ToolbarItems;
			}
		}
	}
}
