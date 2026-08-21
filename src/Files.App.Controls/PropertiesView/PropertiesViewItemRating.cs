// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

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
			if ( e.NewValue != e.OldValue )
			{ 
				control.UpdateRatingProperty( (int)e.NewValue );
			}
		}

		private void UpdateRatingProperty(int newRating)
		{
			double _convertedStars;

			_convertedStars = ConvertToStars( newRating );

			ApplyRatingPropertyToRatingControl( newRating );
		}

		#endregion




		private bool _isLoaded;
		public PropertiesViewItemRating()
		{
			DefaultStyleKey = typeof( PropertiesViewItemRating );

			this.Loaded += (s , e) =>
			{
				_isLoaded = true;
				UpdateRatingProperty( (int)Rating );
			};
		}




		private RatingControl? _ratingControl;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();


			// Remove ValueChanged event hook if RatingControl Part is not found or loaded
			if ( _ratingControl != null )
			{
				_ratingControl.ValueChanged -= OnRatingValueChanged;
			}


			// Get Template Part
			_ratingControl = GetTemplateChild( RatingControlPartName ) as RatingControl;


			// Create ValueChanged event hook if RatingControl Part is found
			if ( _ratingControl != null )
			{
				_ratingControl.ValueChanged += OnRatingValueChanged;
			}
		}




		private void OnRatingValueChanged(RatingControl sender , object args)
		{
			if ( sender == null )
			{
				return;
			}

			int convertedRating;

			// Handle the value sent from the RatingControl
			convertedRating = ConvertToInt( (double)sender.Value );

			this.Rating = convertedRating;
		}




		private double ConvertToStars(int value)
		{
			// The system gives us ratings as an int value btween 1-99
			// we then have to convert this into 1-5 stars
			//  1-12 - 1 Star
			// 13-37 - 2 Stars
			// 38-62 - 3 Stars
			// 63-87 - 4 Stars
			// 88-99 - 5 Stars

			if ( value <= 0 )
				return 0.0;

			if ( value >= 100 )
				return 6.0;

			// If the rating should be 1 Star
			if ( value >= 1 && value <= 12 )
				return 1.0;

			// If the rating should be 2 Stars
			if ( value >= 13 && value <= 37 )
				return 2.0;

			// If the rating should be 3 Stars
			if ( value >= 38 && value <= 62 )
				return 3.0;

			// If the rating should be 4 Stars
			if ( value >= 63 && value <= 87 )
				return 4.0;

			// If the rating should be 5 Stars
			if ( value >= 88 && value <= 99 )
				return 5.0;

			else
			{
				return 0.0;
			}

		}




		private int ConvertToInt(double stars)
		{
			// When setting this value using a Rating controls
			// we assign values as listed below
			// 1 Star  - 1
			// 2 Stars - 25
			// 3 Stars - 50
			// 4 Stars - 75
			// 5 Stars - 99

			if ( stars <= 0.0 )
				return 0;

			if ( stars > 5.0 )
				return 99;

			// If the rating should be 1 Star
			if ( stars == 1.0 )
				return 1;

			// If the rating should be 2 Stars
			if ( stars == 2.0 )
				return 25;

			// If the rating should be 3 Stars
			if ( stars == 3.0 )
				return 50;

			// If the rating should be 4 Stars
			if ( stars == 4.0 )
				return 75;

			// If the rating should be 5 Stars
			if ( stars == 5.0 )
				return 99;

			else 
			{
				return 0;
			}

		}




		private void ApplyRatingPropertyToRatingControl(int newRating)
		{
			if ( _ratingControl == null )
				return;

			double stars = ConvertToStars(newRating);

			if ( stars <= 0 )
			{
				_ratingControl.Value = 0.0;
				_ratingControl.ClearValue( RatingControl.ValueProperty );
			}
			else if ( stars >= 6 )
			{
				_ratingControl.Value = 5;
			}
			else
			{
				_ratingControl.Value = stars;
			}
		}

	}
}
