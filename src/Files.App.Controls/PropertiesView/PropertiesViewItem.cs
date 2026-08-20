using System;
using System.Collections.Generic;
using System.Text;




namespace Files.App.Controls.Primitives
{
	public partial class PropertiesViewItem : Control
	{
		private bool _isLoaded;

		public PropertiesViewItem()
		{

			this.Loaded += (s , e) =>
			{
				_isLoaded = true;
				UpdateOrientationState( Orientation );
				UpdateCanEditState( CanEdit );
			};
		}


		#region Property Changed
		private static void OnOrientationChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesViewItem)d;
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
			var control = (PropertiesViewItem)d;
			control.UpdateCanEditState( (bool)e.NewValue );
		}

		private void UpdateCanEditState(bool canEdit)
		{
			if ( canEdit == true )
			{
				VisualStateManager.GoToState( this , "EditableState" , false );
			}
			else
			{
				VisualStateManager.GoToState( this , "NonEditableState" , false );
			}
		}

		#endregion

	}
}
