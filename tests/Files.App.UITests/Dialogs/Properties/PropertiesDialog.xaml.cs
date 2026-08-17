using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using Files.App.UITests.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Files.App.UITests.Dialogs.Properties.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App.UITests.Dialogs
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class PropertiesDialog : Window
	{
		public PropertiesDialog()
		{
			InitializeComponent();

			OverlappedPresenter presenter = OverlappedPresenter.Create();

			this.ExtendsContentIntoTitleBar = true;
			AppWindow.Resize( new Windows.Graphics.SizeInt32( 640 , 480 ) );
			presenter.IsMaximizable = false;
			AppWindow.SetPresenter( presenter );
			AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
			presenter.PreferredMinimumHeight = 600;
			presenter.PreferredMinimumWidth = 800;

			if ( propertiesFrame != null )
			{
				Page generalPage = new PropertiesGeneralPage();

				if ( generalPage != null )
				{
					propertiesFrame.Navigate( typeof( PropertiesGeneralPage ) );
				}
			}
		}
	}
}
