using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Files.App.Controls.Primitives;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App.Controls
{
	#region Template Parts

	[TemplatePart( Name = RatingControlPartName , Type = typeof( RatingControl ) )]

	#endregion



	public sealed partial class PropertiesViewItemRating : PropertiesViewItem
	{
		#region Constants
		internal const string RatingControlPartName = "PART_RatingControl";

		#endregion




		#region Properties

		public int Rating
		{
			get { return (int)GetValue( RatingProperty ); }
			set { SetValue( RatingProperty , value ); }
		}

		public static readonly DependencyProperty RatingProperty =
			DependencyProperty.Register(
				"Rating",
				typeof(int),
				typeof(PropertiesViewItemRating),
				new PropertyMetadata( 3 , OnRatingPropertyChanged ) );

		#endregion




		#region Property Changed

		private static void OnRatingPropertyChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
		{
			var control = (PropertiesViewItemRating)d;
			control.UpdateRatingProperty( (int)e.NewValue , (int)e.OldValue );
		}

		private void UpdateRatingProperty(int newRating, int oldRating)
		{
			// The system gives us ratings as an int value btween 1-99
			// we then have to convert this into 1-5 stars
			//  1-12 - 1 Star
			// 13-37 - 2 Stars
			// 38-62 - 3 Stars
			// 63-87 - 4 Stars
			// 88-99 - 5 Stars

			// When setting this value using a Rating controls
			// we assign values as listed below
			// 1 Star  - 1
			// 2 Stars - 25
			// 3 Stars - 50
			// 4 Stars - 75
			// 5 Stars - 99

			// If the rating has changed, take action
			if ( newRating != oldRating )
			{
				if ( newRating <= 0 )
				{ }

				if ( newRating >= 100 )
				{ }

				// If the rating should be 1 Star
				if ( newRating >= 1 && newRating <= 12 )
				{ }

				// If the rating should be 2 Stars
				if ( newRating >= 13 && newRating <= 37 )
				{ }

				// If the rating should be 3 Stars
				if ( newRating >= 38 && newRating <= 62 )
				{ }

				// If the rating should be 4 Stars
				if ( newRating >= 63 && newRating <= 87 )
				{ }

				// If the rating should be 5 Stars
				if ( newRating >= 88 && newRating <= 99 )
				{ }
			}
		}

		#endregion


		public PropertiesViewItemRating()
		{
			DefaultStyleKey = typeof( PropertiesViewItemText );
		}
	}
}
