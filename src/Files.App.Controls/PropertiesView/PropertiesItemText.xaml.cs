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
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App.Controls
{
	public sealed partial class PropertiesItemText : UserControl
	{
		public string Label
		{
			get { return (string)GetValue( LabelProperty ); }
			set { SetValue( LabelProperty , value ); }
		}

		public static readonly DependencyProperty LabelProperty =
			DependencyProperty.Register(
				"Label",
				typeof(string),
				typeof(PropertiesItemText),
				new PropertyMetadata( string.Empty ) );




		public string Text
		{
			get { return (string)GetValue( TextProperty ); }
			set { SetValue( TextProperty , value ); }
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(PropertiesItemText),
				new PropertyMetadata( string.Empty ) );




		public GridLength LabelWidth
		{
			get { return (GridLength)GetValue( LabelWidthProperty ); }
			set { SetValue( LabelWidthProperty , value ); }
		}

		public static readonly DependencyProperty LabelWidthProperty =
			DependencyProperty.Register(
				"LabelWidth",
				typeof(GridLength),
				typeof(PropertiesItemText),
				new PropertyMetadata(new GridLength(80, GridUnitType.Pixel)));




		public bool CanEdit
		{
			get { return (bool)GetValue( CanEditProperty ); }
			set { SetValue( CanEditProperty , value ); }
		}

		public static readonly DependencyProperty CanEditProperty =
			DependencyProperty.Register(
				"CanEdit",
				typeof(bool),
				typeof(PropertiesItemText),
				new PropertyMetadata( false, OnCanEditPropertyChanged ) );




		public Orientation Orientation
		{
			get => (Orientation)GetValue( OrientationProperty );
			set => SetValue( OrientationProperty , value );
		}

		public static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register(
				"Orientation",
				typeof(Orientation),
				typeof(PropertiesItemText),
				new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));




		private bool _isLoaded;

		public PropertiesItemText()
		{
			InitializeComponent();

			this.Loaded += (s , e) =>
			{
				_isLoaded = true;
				UpdateOrientationState( Orientation );
				UpdateCanEditState( CanEdit );
			};
		}




		private static void OnOrientationChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesItemText)d;
			control.UpdateOrientationState( (Orientation)e.NewValue );
		}


		private void UpdateOrientationState(Orientation orientation)
		{
			if ( orientation == Orientation.Horizontal )
			{
				VisualStateManager.GoToState( this , "HorizontalState" , false );
			}
			else
			{
				VisualStateManager.GoToState( this , "VerticalState" , false );
			}
		}




		private static void OnCanEditPropertyChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesItemText)d;
			control.UpdateCanEditState( (bool)e.NewValue );
		}


		private void UpdateCanEditState(bool canEdit)
		{
			if ( canEdit == true)
			{
				VisualStateManager.GoToState( this , "EditableState" , false );
			}
			else
			{
				VisualStateManager.GoToState( this , "NonEditableState" , false );
			}
		}
	}
}
