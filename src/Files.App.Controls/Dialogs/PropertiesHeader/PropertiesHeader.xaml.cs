using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App.Controls.Dialogs
{
	public sealed partial class PropertiesHeader : UserControl
	{
		public IconElement Icon
		{
			get { return (IconElement)GetValue( IconProperty ); }
			set { SetValue( IconProperty , value ); }
		}

		public static readonly DependencyProperty IconProperty =
			DependencyProperty.Register(
				"Icon",
				typeof(IconElement),
				typeof(PropertiesHeader),
				new PropertyMetadata( null ) );




		public string Text
		{
			get { return (string)GetValue( TextProperty ); }
			set { SetValue( TextProperty , value ); }
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(PropertiesHeader),
				new PropertyMetadata( string.Empty ) );



		public PropertiesHeader()
		{
			this.InitializeComponent();
		}
	}
}
