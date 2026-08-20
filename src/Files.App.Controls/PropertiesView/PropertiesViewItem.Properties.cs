using System;
using System.Collections.Generic;
using System.Text;




namespace Files.App.Controls.Primitives
{
	public partial class PropertiesViewItem : Control
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
				typeof(PropertiesViewItem),
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
				typeof(PropertiesViewItem),
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
				typeof(PropertiesViewItem),
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
				typeof(PropertiesViewItem),
				new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));


	}
}
