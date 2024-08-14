// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;

namespace Files.App.Controls
{
    /// <summary>
    /// StorageRing - Takes a set of values, converts them to a percentage
    /// and displays it on a ring.
    /// </summary>
    public partial class StorageRing : RangeBase
    {
        #region 1. Private Variables

        private double              _containerSize;         // Size of the inner container after padding
        private double              _containerCenter;       // Center X and Y value of the inner container
        private double              _sharedRadius;          // Radius to be shared by both rings (smaller of the two)

        private double              _oldValue;              // Stores the previous Value
        private double              _oldValueAngle;         // Stored the old ValueAngle

        private double              _valueRingThickness;    // The stored value ring thickness
        private double              _trackRingThickness;    // The stored track ring thickness
        private ThicknessCheck      _thicknessCheck;        // Determines how the two ring thicknesses compare
        private double              _largerThickness;       // The larger of the two ring thicknesses
        private double              _smallerThickness;      // The smaller of the two ring thicknesses

        private Grid?               _containerGrid;         // Reference to the container Grid
        private RingShape?          _valueRingShape;        // Reference to the Value RingShape
        private RingShape?          _trackRingShape;        // Reference to the Track RingShape

        private RectangleGeometry?  _clipRect;              // Clipping RectangleGeometry for the canvas

        private double              _normalisedMinAngle;    // Stores the normalised Minimum Angle
        private double              _normalisedMaxAngle;    // Stores the normalised Maximum Angle
        private double              _gapAngle;              // Stores the angle to be used to separate Value and Track rings
        private double              _validStartAngle;       // The validated StartAngle

        #endregion




        #region 2. Private Setters

        /// <summary>
        /// Sets the Container size to the smaller of control's Height and Width.
        /// </summary>
        private void SetContainerSize(double controlWidth , double controlHeight , Thickness padding)
        {
            double correctedWidth = controlWidth - (padding.Left + padding.Right);
            double correctedHeight = controlHeight - (padding.Top + padding.Bottom);

            double check = Math.Min(correctedWidth, correctedHeight);
            double minSize = 8;

            if ( check < minSize )
            {
                _containerSize = minSize;
            }
            else
            {
                _containerSize = check;
            }
        }




        /// <summary>
        /// Sets the private Container center X and Y value
        /// </summary>
        private void SetContainerCenter(double containerSize)
        {
            _containerCenter = ( containerSize / 2 );
        }




        /// <summary>
        /// Sets the shared Radius by passing in containerSize and thickness.
        /// </summary>
        private void SetSharedRadius(double containerSize , double thickness)
        {
            double check = (containerSize / 2) - (thickness / 2);
            double minSize = 4;

            if ( check <= minSize )
            {
                _sharedRadius = minSize;
            }
            else
            {
                _sharedRadius = check;
            }
        }




        /// <summary>
        /// Sets the private old Value
        /// </summary>
        private void SetOldValue(double value)
        {
            _oldValue = value;
        }




        /// <summary>
        /// Sets the private old ValueAngle
        /// </summary>
        private void SetOldValueAngle(double value)
        {
            _oldValueAngle = value;
        }




        /// <summary>
        /// Sets the private Value Ring Thickness
        /// </summary>
        private void SetValueRingThickness(double value)
        {
            _valueRingThickness = value;
        }




        /// <summary>
        /// Sets the private Track Ring Thickness
        /// </summary>
        private void SetTrackRingThickness(double value)
        {
            _trackRingThickness = value;
        }




        /// <summary>
        /// Sets the private ThicknessCheck enum value
        /// </summary>
        private void SetThicknessCheck(double valueThickness , double trackThickness)
        {
            if ( valueThickness > trackThickness )
            {
                _thicknessCheck = ThicknessCheck.Value;
            }
            else if ( valueThickness < trackThickness )
            {
                _thicknessCheck = ThicknessCheck.Track;
            }
            else
            {
                _thicknessCheck = ThicknessCheck.Equal;
            }
        }




        /// <summary>
        /// Sets the private LargerThickness value
        /// </summary>
        private void SetLargerThickness(double value)
        {
            _largerThickness = value;
        }




        /// <summary>
        /// Sets the private SmallerThickness value
        /// </summary>
        private void SetSmallerThickness(double value)
        {
            _smallerThickness = value;
        }




        /// <summary>
        /// Sets the private Container Grid reference
        /// </summary>
        private void SetContainerGrid(Grid grid)
        {
            _containerGrid = grid;
        }




        /// <summary>
        /// Sets the private Value RingShape reference
        /// </summary>
        private void SetValueRingShape(RingShape ringShape)
        {
            _valueRingShape = ringShape;
        }




        /// <summary>
        /// Sets the private Track RingShape reference
        /// </summary>
        private void SetTrackRingShape(RingShape ringShape)
        {
            _trackRingShape = ringShape;
        }




        /// <summary>
        /// Sets the clipping RectangleGeometry for the Canvas
        /// </summary>
        private void SetClippingRectGeo(RectangleGeometry clipRectGeo)
        {
            _clipRect = clipRectGeo;
        }




        /// <summary>
        /// Sets the private Normalized min angle
        /// </summary>
        private void SetNormalisedMinAngle(double angle)
        {
            _normalisedMinAngle = angle;
        }




        /// <summary>
        /// Sets the private Normalized max angle
        /// </summary>
        private void SetNormalisedMaxAngle(double angle)
        {
            _normalisedMaxAngle = angle;
        }




        /// <summary>
        /// Sets the private Gap angle
        /// </summary>
        private void SetGapAngle(double angle)
        {
            _gapAngle = angle;
        }




        /// <summary>
        /// Sets the private _validStartAngle value
        /// </summary>
        /// <param name="validStartAngle">The validated StartAngle</param>
        private void SetValidStartAngle(double validStartAngle)
        {
            _validStartAngle = validStartAngle;
        }

        #endregion




        #region 3. Private Getters

        /// <summary>
        /// Gets the Container size
        /// </summary>
        double GetContainerSize()
        {
            return _containerSize;
        }




        /// <summary>
        /// Gets the Container Center
        /// </summary>
        double GetContainerCenter()
        {
            return _containerCenter;
        }




        /// <summary>
        /// Gets the Shared Radius
        /// </summary>
        double GetSharedRadius()
        {
            return _sharedRadius;
        }




        /// <summary>
        /// Gets the old Value
        /// </summary>
        double GetOldValue()
        {
            return _oldValue;
        }




        /// <summary>
        /// Gets the old ValueAngle
        /// </summary>
        double GetOldValueAngle()
        {
            return _oldValueAngle;
        }




        /// <summary>
        /// Gets the Value Ring Thickness
        /// </summary>
        double GetValueRingThickness()
        {
            return _valueRingThickness;
        }




        /// <summary>
        /// Gets the Track Ring Thickness
        /// </summary>
        double GetTrackRingThickness()
        {
            return _trackRingThickness;
        }




        /// <summary>
        /// Gets the ThicknessCheck enum value
        /// </summary>
        ThicknessCheck GetThicknessCheck()
        {
            return _thicknessCheck;
        }




        /// <summary>
        /// Gets the Larger Thickness
        /// </summary>
        double GetLargerThickness()
        {
            return _largerThickness;
        }




        /// <summary>
        /// Gets the Smaller Thickness
        /// </summary>
        double GetSmallerThickness()
        {
            return _smallerThickness;
        }




        /// <summary>
        /// Gets the Container Grid reference
        /// </summary>
        Grid? GetContainerGrid()
        {
            return _containerGrid;
        }




        /// <summary>
        /// Gets the Value RingShape reference
        /// </summary>
        RingShape? GetValueRingShape()
        {
            return _valueRingShape;
        }




        /// <summary>
        /// Gets the Track RingShape reference
        /// </summary>
        RingShape? GetTrackRingShape()
        {
            return _trackRingShape;
        }




        /// <summary>
        /// Gets the clipping RectangleGeometry reference
        /// </summary>
        RectangleGeometry? GetClippingRectGeo()
        {
            return _clipRect;
        }




        /// <summary>
        /// Gets the Normalised Minimuim Angle
        /// </summary>
        double GetNormalisedMinAngle()
        {
            return _normalisedMinAngle;
        }




        /// <summary>
        /// Gets the Normalised Maximum Angle
        /// </summary>
        double GetNormalisedMaxAngle()
        {
            return _normalisedMaxAngle;
        }




        /// <summary>
        /// Gets the Gap Angle
        /// </summary>
        double GetGapAngle()
        {
            return _gapAngle;
        }




        /// <summary>
        /// Gets the private stored _validStartAngle value
        /// </summary>
        /// <returns>The validated StartAngle</returns>
        private double GetValidStartAngle()
        {
            return _validStartAngle;
        }

        #endregion




        #region 4. Initialisation

        /// <summary>
        /// Applies an implicit Style of a matching TargetType
        /// </summary>
        public StorageRing()
        {
            SizeChanged      -= StorageRing_SizeChanged;
            Unloaded         -= StorageRing_Unloaded;
            IsEnabledChanged -= StorageRing_IsEnabledChanged;
            Loaded           -= StorageRing_Loaded;

            DefaultStyleKey   = typeof( StorageRing );

            SizeChanged      += StorageRing_SizeChanged;
            Unloaded	     += StorageRing_Unloaded;
            IsEnabledChanged += StorageRing_IsEnabledChanged;
            Loaded           += StorageRing_Loaded;
        }




        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            InitialiseParts();

            base.OnApplyTemplate();
        }




        /// <summary>
        /// Initialises the Parts and properties of a PercentageRing control.
        /// </summary>
        private void InitialiseParts()
        {
            // Retrieve references to visual elements
            SetContainerGrid( GetTemplateChild( ContainerPartName ) as Grid );

            SetValueRingShape( GetTemplateChild( ValueRingShapePartName ) as RingShape );
            SetTrackRingShape( GetTemplateChild( TrackRingShapePartName ) as RingShape );

            CalculateAndSetNormalisedAngles( this , this.MinAngle , this.MaxAngle );

            UpdateValues( this , this.Value , 0.0, false, -1.0);
        }

        #endregion




        #region 5. Property Change Events

        /// <summary>
        /// Occurs when either the ActualHeight or the ActualWidth property changes value,
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Provides data related to the SizeChanged event.</param>
        private void StorageRing_SizeChanged(object sender , SizeChangedEventArgs e)
        {
            StorageRing storageRing = (StorageRing)sender;

            UpdateContainerCenterAndSizes( storageRing , e.NewSize );
            
            UpdateRings( storageRing );
        }




        /// <summary>
        /// Occurs when this object is no longer connected to the value object tree.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Provides data related to the Unloaded event.</param>
        private void StorageRing_Unloaded(object sender , RoutedEventArgs e)
        {
            SizeChanged      -= StorageRing_SizeChanged;
            Unloaded		 -= StorageRing_Unloaded;
            IsEnabledChanged -= StorageRing_IsEnabledChanged;
        }




        /// <summary>
        /// Occurs when this object is loaded into the value object tree
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Provides data related to the Unloaded event.</param>
        private void StorageRing_Loaded(object sender , RoutedEventArgs e)
        {
            StorageRing storageRing = (StorageRing)sender;
        }




        /// <summary>
        /// Occurs when the IsEnabled property changes.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Provides data for a PropertyChangedCallback implementation that is invoked 
        /// when a dependency property changes its value. </param>
        private void StorageRing_IsEnabledChanged(object sender , DependencyPropertyChangedEventArgs e)
        {
            StorageRing storageRing = (StorageRing)sender;

            UpdateVisualState( storageRing );
        }




        /// <summary>
        /// Occurs when the ValueRingThickness property value changes.
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newValueRingThickness">New ValueRing Thickness</param>
        private void ValueRingThicknessChanged(DependencyObject d , double newValueRingThickness)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateRingThickness( storageRing , newValueRingThickness , false );

            UpdateRings( storageRing );
        }




        /// <summary>
        /// Occurs when the TrackRingThickness property value changes.
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newTrackRingThickness">New TrackRing Thickness</param>
        private void TrackRingThicknessChanged(DependencyObject d , double newTrackRingThickness)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateRingThickness( storageRing , newTrackRingThickness, true );

            UpdateRings( storageRing );
        }




        /// <summary>
        /// Occurs when the Percent property value changes.
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newPercent">New Percent value</param>
        private void PercentChanged(DependencyObject d , double newPercent)
        {
            StorageRing storageRing = (StorageRing)d;

            double adjustedPercentage;

            if ( newPercent <= 0.0 )
            {
                adjustedPercentage = 0.0;
            }
            else if ( newPercent <= 100.0 )
            {
                adjustedPercentage = 100.0;
            }
            else
            {
                adjustedPercentage = newPercent;
            }

            UpdateValues( storageRing , storageRing.Value , storageRing.GetOldValue(), true, adjustedPercentage );

            UpdateVisualState( storageRing );

            UpdateRings( storageRing );
        }




        /// <summary>
        /// Occurs when the PercentCaution property value changes.
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newPercentCaution">New PercentCaution value</param>
        private void PercentCautionChanged(DependencyObject d , double newPercentCaution)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateValues( storageRing , storageRing.Value , storageRing.GetOldValue(), false, -1.0 );

            UpdateVisualState( storageRing );

            UpdateRings( storageRing );
        }




        /// <summary>
        /// Occurs when the PercentCritical property value changes.
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newPercentCritical">The new PercentCritical value</param>
        private void PercentCriticalChanged(DependencyObject d , double newPercentCritical)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateValues( storageRing , storageRing.Value , storageRing.GetOldValue() , false , -1.0 );

            UpdateVisualState( storageRing );

            UpdateRings( storageRing );
        }




        /// <summary>
        /// Occurs when the StartAngle property value changes
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newAngle">The new StartAngle</param>
        private void StartAngleChanged(DependencyObject d , double newAngle)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateValues( storageRing , storageRing.Value , storageRing.GetOldValue() , false , -1.0 );

            CalculateAndSetNormalisedAngles( storageRing , storageRing.MinAngle , newAngle );

            ValidateStartAngle( storageRing , newAngle );

            UpdateRings( storageRing );
        }




        /// <summary>
        /// Occurs when the MinAngle property value changes
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newAngle">The new MinAngle</param>
        private void MinAngleChanged(DependencyObject d , double newAngle)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateValues( storageRing , storageRing.Value , storageRing.GetOldValue() , false , -1.0 );

            CalculateAndSetNormalisedAngles( storageRing , newAngle , storageRing.MaxAngle );

            UpdateRings( storageRing );
        }




        /// <summary>
        /// Occurs when the MaxAngle property value changes
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newAngle">The new MaxAngle</param>
        private void MaxAngleChanged(DependencyObject d , double newAngle)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateValues( storageRing , storageRing.Value , storageRing.GetOldValue() , false , -1.0 );

            CalculateAndSetNormalisedAngles( storageRing , storageRing.MinAngle , newAngle );

            UpdateRings( storageRing );
        }

        #endregion




        #region 6. Update functions

        /// <summary>
        /// Updates Values used by the control
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="newValue">The new Value</param>
        /// <param name="oldValue">The old Value</param>
        /// <param name="percentChanged">Checks if Percent value is being changed</param>
        /// <param name="newPercent">The new Percent value</param>
        private void UpdateValues(DependencyObject d , double newValue , double oldValue, bool percentChanged, double newPercent)
        {
            StorageRing storageRing = (StorageRing)d;

            CalculateAndSetNormalisedAngles( storageRing , storageRing.MinAngle , storageRing.MaxAngle );

            var normalisedMinAngle = storageRing.GetNormalisedMinAngle();
            var normalisedMaxAngle = storageRing.GetNormalisedMaxAngle();

            double adjustedValue;

            if ( percentChanged )
            {
                double percentToValue;

                percentToValue = PercentageToValue(newPercent, storageRing.Minimum, storageRing.Maximum);
                
                adjustedValue = percentToValue;
            }
            else
            {
                adjustedValue = newValue;
            }

            ValueAngle = DoubleToAngle( adjustedValue , storageRing.Minimum , storageRing.Maximum , normalisedMinAngle , normalisedMaxAngle );
            Percent = DoubleToPercentage( adjustedValue , storageRing.Minimum , storageRing.Maximum );

            SetOldValue( oldValue );
            SetOldValueAngle( DoubleToAngle( oldValue , storageRing.Minimum , storageRing.Maximum , normalisedMinAngle , normalisedMaxAngle ) );
        }




        /// <summary>
        /// Updates Container Center point and Sizes
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="newSize">The new Size</param>
        private void UpdateContainerCenterAndSizes(DependencyObject d , Size newSize)
        {
            StorageRing storageRing = (StorageRing)d;

            var borderThickness = storageRing.BorderThickness;

            var borderWidth = borderThickness.Left + borderThickness.Right;
            var borderHeight = borderThickness.Top + borderThickness.Bottom;

            // Set Container Size
            SetContainerSize( storageRing.Width - ( borderWidth * 2) , storageRing.Height - (borderHeight * 2) , storageRing.Padding );
            storageRing.AdjustedSize = GetContainerSize();

            // Set Container Center
            SetContainerCenter( storageRing.AdjustedSize );

            // Set Clipping Rectangle
            RectangleGeometry rectGeo = new RectangleGeometry();
            rectGeo.Rect = new Rect( 0 - borderWidth , 0 - borderHeight , AdjustedSize + borderWidth , AdjustedSize + borderHeight);
            SetClippingRectGeo( rectGeo );

            // Get Container
            var container = GetContainerGrid();

            // If the Container is not null.
            if ( container != null )
            {
                // Set the _container width and height to the adjusted size (AdjustedSize).
                container.Width = storageRing.AdjustedSize;
                container.Height = storageRing.AdjustedSize;
                container.Clip = GetClippingRectGeo();
            }
        }




        /// <summary>
        /// Updates the Radii of both Rings
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="newRadius">The new Radius</param>
        /// <param name="isTrack">Checks if the Track is currently being updated</param>
        private void UpdateRadii(DependencyObject d , double newRadius , bool isTrack)
        {
            StorageRing storageRing = (StorageRing)d;

            double valueRingThickness = storageRing.GetValueRingThickness();
            double trackRingThickness = storageRing.GetTrackRingThickness();

            // We want to limit the Thickness values to no more than 1/5 of the container size
            if ( isTrack == false )
            {
                if ( newRadius > ( storageRing.AdjustedSize / 5 ) )
                {
                    valueRingThickness = ( storageRing.AdjustedSize / 5 );
                }
                else
                {
                    valueRingThickness = newRadius;
                }
            }
            else
            {
                if ( newRadius > ( storageRing.AdjustedSize / 5 ) )
                {
                    trackRingThickness = ( storageRing.AdjustedSize / 5 );
                }
                else
                {
                    trackRingThickness = newRadius;
                }
            }

            // We check if both Rings have Equal thickness
            if ( storageRing.GetThicknessCheck() == ThicknessCheck.Equal )
            {
                storageRing.SetSharedRadius( storageRing.AdjustedSize , 0 );
            }
            // Else we use the larger thickness to adjust the size
            else
            {
                storageRing.SetSharedRadius( storageRing.AdjustedSize , GetLargerThickness() );
            }

        }




        /// <summary>
        /// Updates the Thickness for both Rings
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="newThickness">The new Thickness</param>
        /// <param name="isTrack">Checks if the TrackRing Thickness is being updated</param>
        private void UpdateRingThickness(DependencyObject d , double newThickness , bool isTrack)
        {
            StorageRing storageRing = (StorageRing)d;

            if ( isTrack == false )
            {
                SetValueRingThickness( newThickness );
            }
            else
            {
                SetTrackRingThickness( newThickness );
            }

            SetThicknessCheck( GetValueRingThickness() , GetTrackRingThickness() );

            if ( GetThicknessCheck() == ThicknessCheck.Value )
            {
                SetLargerThickness( GetValueRingThickness() );
                SetSmallerThickness( GetTrackRingThickness() );
            }
            else if ( GetThicknessCheck() == ThicknessCheck.Track )
            {
                SetLargerThickness( GetTrackRingThickness() );
                SetSmallerThickness( GetValueRingThickness() );
            }
            else // ThicknessCheck == Equal
            {
                SetLargerThickness( GetValueRingThickness() );
                SetSmallerThickness( GetValueRingThickness() );
            }
        }




        /// <summary>
        /// Updates the GapAngle separating both Rings.
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="newRadius">The new Radius</param>
        /// <param name="isTrack">Checks if the TrackRing is being updated</param>
        private void UpdateGapAngle(DependencyObject d , double newRadius , bool isTrack)
        {
            StorageRing storageRing = (StorageRing)d;

            double angle = storageRing.GapThicknessToAngle(storageRing.GetSharedRadius(), ( storageRing.GetLargerThickness() * 0.75 ) );

            SetGapAngle( angle );
        }




        /// <summary>
        /// Updates the control's VisualState
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        private void UpdateVisualState(DependencyObject d)
        {
            StorageRing storageRing = (StorageRing)d;

            // First is the control is Disabled
            if ( storageRing.IsEnabled == false )
            {
                VisualStateManager.GoToState( this , DisabledStateName , true );
            }
            // Then the control is Enabled
            else
            {
                // Is the Percent value equal to or above the PercentCritical value
                if ( storageRing.Percent >= storageRing.PercentCritical )
                {
                    VisualStateManager.GoToState( this , CriticalStateName , true );
                }
                // Is the Percent value equal to or above the PercentCaution value
                else if ( storageRing.Percent >= storageRing.PercentCaution )
                {
                    VisualStateManager.GoToState( this , CautionStateName , true );
                }
                // Else we use the Safe State
                else
                {
                    VisualStateManager.GoToState( this , SafeStateName , true );
                }
            }
        }

        #endregion




        #region 7. Update Rings

        /// <summary>
        /// Updates both Rings
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        private void UpdateRings(DependencyObject d)
        {
            StorageRing storageRing = (StorageRing)d;

            var valueRingShape = GetValueRingShape();
            var trackRingShape = GetTrackRingShape();

            if ( valueRingShape == null || trackRingShape == null )
            {
                return;
            }

            UpdateContainerCenterAndSizes( storageRing , DesiredSize );

            CalculateAndSetNormalisedAngles( storageRing , storageRing.MinAngle , storageRing.MaxAngle );

            UpdateRingSizes( d , valueRingShape , trackRingShape );

            UpdateRadii( d , storageRing.GetSharedRadius() , false );
            UpdateRadii( d , storageRing.GetSharedRadius() , true );

            UpdateGapAngle( d , storageRing.GetSharedRadius() , false );

            UpdateRingLayouts( d , valueRingShape , trackRingShape );

            UpdateRingAngles( d , valueRingShape , trackRingShape );

            UpdateRingStrokes( d , valueRingShape , trackRingShape );

            UpdateVisualState( d );
        }




        /// <summary>
        /// Updates the Ring Sizes
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="valueRingShape">The reference to the ValueRing RingShape TemplatePart</param>
        /// <param name="trackRingShape">The reference to the TrackRing RingShape TemplatePart</param>
        private void UpdateRingSizes(DependencyObject d , RingShape valueRingShape , RingShape trackRingShape)
        {
            StorageRing storageRing = (StorageRing)d;

            if ( valueRingShape == null || trackRingShape == null )
            {
                return;
            }

            // Set sizes for the rings as needed
            if ( storageRing.GetThicknessCheck() == ThicknessCheck.Value )
            {
                valueRingShape.Width = storageRing.GetContainerSize();
                valueRingShape.Height = storageRing.GetContainerSize();

                trackRingShape.Width = storageRing.GetContainerSize() - ( storageRing.GetLargerThickness() / 2 );
                trackRingShape.Height = storageRing.GetContainerSize() - ( storageRing.GetLargerThickness() / 2 );
            }
            else if ( storageRing.GetThicknessCheck() == ThicknessCheck.Track )
            {
                valueRingShape.Width = storageRing.GetContainerSize() - ( storageRing.GetLargerThickness() / 2 );
                valueRingShape.Height = storageRing.GetContainerSize() - ( storageRing.GetLargerThickness() / 2 );

                trackRingShape.Width = storageRing.GetContainerSize();
                trackRingShape.Height = storageRing.GetContainerSize();
            }
            else // ThicknessCheck == Equal
            {
                valueRingShape.Width = storageRing.GetContainerSize();
                valueRingShape.Height = storageRing.GetContainerSize();

                trackRingShape.Width = storageRing.GetContainerSize();
                trackRingShape.Height = storageRing.GetContainerSize();
            }

            valueRingShape.UpdateLayout();
            trackRingShape.UpdateLayout();
        }




        /// <summary>
        /// Updates the Start and End Angles for both Rings
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="valueRingShape">The reference to the ValueRing RingShape TemplatePart</param>
        /// <param name="trackRingShape">The reference to the TrackRing RingShape TemplatePart</param>
        private void UpdateRingAngles(DependencyObject d , RingShape valueRingShape , RingShape trackRingShape)
        {
            StorageRing storageRing = (StorageRing)d;

            if ( valueRingShape == null || trackRingShape == null )
            {
                return;
            }

            var normalisedMinAngle = GetNormalisedMinAngle();
            var normalisedMaxAngle = GetNormalisedMaxAngle();

            double gapAngle = storageRing.GetGapAngle();

            double ValueStartAngle = normalisedMinAngle;
            double ValueEndAngle;
            double TrackStartAngle = normalisedMaxAngle;
            double TrackEndAngle;

            //
            // We get percentage values to use for manipulating how we draw the rings.
            var minPercent = storageRing.DoubleToPercentage(storageRing.Minimum, storageRing.Minimum, storageRing.Maximum);
            var maxPercent = storageRing.DoubleToPercentage(storageRing.Maximum, storageRing.Minimum, storageRing.Maximum);
            var percent = storageRing.Percent;

            //
            // Percent is below or at its Minimum
            if ( percent <= minPercent )
            {
                ValueEndAngle = normalisedMinAngle;

                TrackStartAngle = normalisedMaxAngle - 0.01;
                TrackEndAngle = normalisedMinAngle;
            }
            //
            // Percent is between it's Minimum and its Minimum + 2 (between 0% and 2%)
            else if ( percent > minPercent && percent < minPercent + 2.0 )
            {
                ValueEndAngle = storageRing.ValueAngle;

                double interpolatedStartTo;
                double interpolatedEndTo;

                //
                // We need to interpolate the track start and end angles between pRing.Minimum and pRing.Minimum + 0.75
                interpolatedStartTo = storageRing.GetAdjustedAngle( storageRing,
                                                                    minPercent ,
                                                                    percent ,
                                                                    minPercent + 2.0 ,
                                                                    normalisedMinAngle ,
                                                                    normalisedMinAngle + gapAngle,
                                                                    storageRing.ValueAngle,
                                                                    true);

                if ( IsFullCircle( normalisedMinAngle , normalisedMaxAngle ) == true )
                {
                    interpolatedEndTo = storageRing.GetAdjustedAngle(   storageRing,
                                                                        minPercent ,
                                                                        percent ,
                                                                        minPercent + 2.0 ,
                                                                        normalisedMaxAngle ,
                                                                        normalisedMaxAngle - ( gapAngle + storageRing.ValueAngle ),
                                                                        storageRing.ValueAngle ,
                                                                        true);
                }
                else
                {
                    interpolatedEndTo = normalisedMaxAngle;
                }

                TrackStartAngle = interpolatedEndTo;
                TrackEndAngle = interpolatedStartTo;
            }
            //
            // Percent is at or above its Maximum value
            else if ( percent >= maxPercent )
            {
                ValueEndAngle = normalisedMaxAngle;

                TrackStartAngle = normalisedMaxAngle;
                TrackEndAngle = normalisedMinAngle;
            }
            //
            // Any value between the Minimum and the Maximum value
            else
            {
                ValueEndAngle = storageRing.ValueAngle;

                if ( IsFullCircle( MinAngle , MaxAngle ) == true )
                {
                    TrackStartAngle = normalisedMaxAngle - gapAngle;

                    //
                    // When the trackRing's EndAngle meets or exceeds its adjusted StartAngle
                    if ( storageRing.ValueAngle > ( normalisedMaxAngle - ( gapAngle * 2 ) ) )
                    {
                        TrackEndAngle = normalisedMaxAngle - ( gapAngle - 0.0001 );
                    }
                    else
                    {
                        // We take the MaxAngle - the GapAngle, then minus the ValueAngle from it
                        TrackEndAngle = ( normalisedMinAngle + gapAngle ) - ( normalisedMinAngle - storageRing.ValueAngle );
                    }
                }
                else
                {
                    TrackStartAngle = normalisedMaxAngle;

                    //
                    // When the trackRing's EndAngle meets or exceeds its adjusted StartAngle
                    if ( storageRing.ValueAngle > ( normalisedMaxAngle - ( gapAngle / 20 ) ) )
                    {
                        TrackEndAngle = ( normalisedMaxAngle - 0.0001 );
                    }
                    else
                    {
                        // We take the MaxAngle - the GapAngle, then minus the ValueAngle from it
                        TrackEndAngle = ( normalisedMinAngle + ( gapAngle - ( normalisedMinAngle - storageRing.ValueAngle ) ) );
                    }
                }
            }

            valueRingShape.StartAngle = ValueStartAngle;
            trackRingShape.StartAngle = TrackStartAngle;

            valueRingShape.EndAngle = ValueEndAngle;
            trackRingShape.EndAngle = TrackEndAngle;
        }




        /// <summary>
        /// Updates the Layout for both Rings
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="valueRingShape">The reference to the ValueRing RingShape TemplatePart</param>
        /// <param name="trackRingShape">The reference to the TrackRing RingShape TemplatePart</param>
        private void UpdateRingLayouts(DependencyObject d , RingShape valueRingShape , RingShape trackRingShape)
        {
            StorageRing storageRing = (StorageRing)d;

            if ( valueRingShape == null || trackRingShape == null )
            {
                return;
            }

            double radius = storageRing.GetSharedRadius();

            valueRingShape.RadiusWidth = radius;
            valueRingShape.RadiusHeight = radius;

            trackRingShape.RadiusWidth = radius;
            trackRingShape.RadiusHeight = radius;

            // Apply Width and Heights
            valueRingShape.Width = storageRing.AdjustedSize;
            valueRingShape.Height = storageRing.AdjustedSize;

            trackRingShape.Width = storageRing.AdjustedSize;
            trackRingShape.Height = storageRing.AdjustedSize;
        }




        /// <summary>
        /// Updates the Strokes for both Rings
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="valueRingShape">The reference to the ValueRing RingShape TemplatePart</param>
        /// <param name="trackRingShape">The reference to the TrackRing RingShape TemplatePart</param>
        private void UpdateRingStrokes(DependencyObject d , RingShape valueRingShape , RingShape trackRingShape)
        {
            StorageRing storageRing = (StorageRing)d;

            if ( valueRingShape == null || trackRingShape == null )
            {
                return;
            }

            var normalisedMinAngle = GetNormalisedMinAngle();
            var normalisedMaxAngle = GetNormalisedMaxAngle();

            //
            // We get percentage values to use for manipulating how we draw the rings.
            var minPercent = storageRing.DoubleToPercentage(storageRing.Minimum, storageRing.Minimum, storageRing.Maximum);
            var maxPercent = storageRing.DoubleToPercentage(storageRing.Maximum, storageRing.Minimum, storageRing.Maximum);
            var percent = storageRing.Percent;

            //
            // Percent is below or at its Minimum
            if ( percent <= minPercent )
            {
                valueRingShape.StrokeThickness = 0;
                trackRingShape.StrokeThickness = storageRing.GetTrackRingThickness();
            }
            //
            // Percent is between it's Minimum and its Minimum + 2.0 (between 0% and 2%)
            else if ( percent > minPercent && percent < minPercent + 2.0 )
            {
                valueRingShape.StrokeThickness = storageRing.GetThicknessTransition( storageRing , 
                                                                                     minPercent , 
                                                                                     percent , 
                                                                                     minPercent + 2.0 , 
                                                                                     0.0 , 
                                                                                     storageRing.GetValueRingThickness() , 
                                                                                     true );
                trackRingShape.StrokeThickness = storageRing.GetTrackRingThickness();
            }
            //
            // Percent is at or above its Maximum value
            else if ( percent >= maxPercent )
            {
                valueRingShape.StrokeThickness = storageRing.GetValueRingThickness();
                trackRingShape.StrokeThickness = 0;
            }
            //
            // Any percent value between the Minimum + 2 and the Maximum value
            else
            {
                valueRingShape.StrokeThickness = storageRing.ValueRingThickness;

                if ( IsFullCircle( normalisedMinAngle , normalisedMaxAngle ) == true )
                {
                    if ( storageRing.ValueAngle > ( normalisedMaxAngle + 1.0 ) - ( storageRing.GetGapAngle() * 2 ) )
                    {
                        valueRingShape.StrokeThickness = storageRing.GetValueRingThickness();
                        trackRingShape.StrokeThickness = storageRing.GetThicknessTransition( storageRing ,
                                                                                            ( normalisedMaxAngle + 0.1 ) - ( storageRing.GetGapAngle() * 2 ) ,
                                                                                              storageRing.ValueAngle , ( normalisedMaxAngle ) - ( storageRing.GetGapAngle() ) ,
                                                                                              storageRing.GetTrackRingThickness() ,
                                                                                              0.0 ,
                                                                                              true );
                    }
                    else
                    {
                        valueRingShape.StrokeThickness = storageRing.GetValueRingThickness();
                        trackRingShape.StrokeThickness = storageRing.GetTrackRingThickness();
                    }
                }
                else
                {
                    if ( storageRing.ValueAngle > ( normalisedMaxAngle - ( storageRing.GetGapAngle() ) ) )
                    {
                        valueRingShape.StrokeThickness = storageRing.GetValueRingThickness();
                        trackRingShape.StrokeThickness = storageRing.GetThicknessTransition( storageRing ,
                                                                                             ( normalisedMaxAngle + 0.1) - ( storageRing.GetGapAngle() / 2 ) ,
                                                                                             storageRing.ValueAngle , ( normalisedMaxAngle ) - ( storageRing.GetGapAngle() / 2 ) ,
                                                                                             storageRing.GetTrackRingThickness() ,
                                                                                             0.0 ,
                                                                                             true );
                    }
                    else
                    {
                        valueRingShape.StrokeThickness = storageRing.GetValueRingThickness();
                        trackRingShape.StrokeThickness = storageRing.GetTrackRingThickness();
                    }
                }
            };
        }

        #endregion




        #region 8. RangeBase Events

        /// <summary>
        /// Occurs when the range value changes.
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newValue">The new Value</param>
        /// <param name="oldValue">The previous Value</param>
        private void StorageRing_ValueChanged(DependencyObject d , double newValue , double oldValue)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateValues(d , newValue , oldValue , false , -1.0 );

            UpdateRings( d );
        }




        /// <summary>
        /// Occurs when the Minimum value changes.
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newValue">The new Minimum value</param>
        private void StorageRing_MinimumChanged(DependencyObject d , double newMinimum)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateRings( d );
        }




        /// <summary>
        /// Occurs when the Maximum value changes.
        /// </summary>
        /// <param name="d">The DependencyObject which holds the DependencyProperty</param>
        /// <param name="newValue">The new Maximum value</param>
        private void StorageRing_MaximumChanged(DependencyObject d , double newMaximum)
        {
            StorageRing storageRing = (StorageRing)d;

            UpdateRings( d );
        }

        #endregion




        #region 9. Conversion methods

        /// <summary>
        /// Calculates and Sets the normalised Min and Max Angles
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="minAngle">MinAngle in the range from -180 to 180.</param>
        /// <param name="maxAngle">MaxAngle, in the range from -180 to 540.</param>
        private static void CalculateAndSetNormalisedAngles(DependencyObject d , double minAngle , double maxAngle)
        {
            StorageRing storageRing = (StorageRing)d;

            var result = CalculateModulus( minAngle , 360 );

            if ( result >= 180 )
            {
                result = result - 360;
            }

            storageRing.SetNormalisedMinAngle( result );

            result = CalculateModulus( maxAngle , 360 );

            if ( result < 180 )
            {
                result = result + 360;
            }

            if ( result > storageRing.GetNormalisedMinAngle() + 360 )
            {
                result = result - 360;
            }


            storageRing.SetNormalisedMaxAngle( result );
        }




        /// <summary>
        /// Calculates the modulus of a number with respect to a divider.
        /// The result is always positive or zero, regardless of the input values.
        /// </summary>
        /// <param name="number">The input number.</param>
        /// <param name="divider">The divider (non-zero).</param>
        /// <returns>The positive modulus result.</returns>
        private static double CalculateModulus(double number , double divider)
        {
            // Calculate the modulus
            var result = number % divider;

            // Ensure the result is positive or zero
            result = result < 0 ? result + divider : result;

            return result;
        }




        /// <summary>
        /// Validates the StartAngle
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="startAngle">The StartAngle to validate</param>
        private void ValidateStartAngle(DependencyObject d , double startAngle)
        {
            StorageRing storageRing = (StorageRing)d;

            if ( startAngle >= GetNormalisedMaxAngle() )
            {
                SetValidStartAngle( GetNormalisedMaxAngle() );
            }
            else if ( startAngle <= GetNormalisedMinAngle() )
            {
                SetValidStartAngle( GetNormalisedMinAngle() );
            }
            else
            {
                SetValidStartAngle( startAngle );
            }
        }




        /// <summary>
        /// Calculates an interpolated thickness value based on the provided parameters.
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="startValue">The starting value for interpolation.</param>
        /// <param name="value">The current value to interpolate.</param>
        /// <param name="endValue">The ending value for interpolation.</param>
        /// <param name="startThickness">The starting thickness value.</param>
        /// <param name="endThickness">The ending thickness value.</param>
        /// <param name="useEasing">Indicates whether to apply an easing function.</param>
        /// <returns>The interpolated thickness value.</returns>
        private double GetThicknessTransition(DependencyObject d , double startValue , double value , double endValue , double startThickness , double endThickness , bool useEasing)
        {
            // Ensure that value is within the range [startValue, endValue]
            value = Math.Max( startValue , Math.Min( endValue , value ) );

            // Calculate the interpolation factor (t) between 0 and 1
            var t = (value - startValue) / (endValue - startValue);

            double interpolatedThickness;

            if ( useEasing )
            {
                // Apply an easing function (e.g., quadratic ease-in-out)
                //var easedT = EaseInOutFunction(t);
                var easedT = EaseOutCubic(t);

                // Interpolate the thickness
                interpolatedThickness = startThickness + easedT * ( endThickness - startThickness );
            }
            else
            {
                // Interpolate the thickness
                interpolatedThickness = startThickness + t * ( endThickness - startThickness );
            }

            return interpolatedThickness;
        }




        /// <summary>
        /// Calculates an interpolated angle based on the provided parameters.
        /// </summary>
        /// <param name="d">The DependencyObject representing the control.</param>
        /// <param name="startValue">The starting value for interpolation.</param>
        /// <param name="value">The current value to interpolate.</param>
        /// <param name="endValue">The ending value for interpolation.</param>
        /// <param name="startAngle">The starting angle value.</param>
        /// <param name="endAngle">The ending angle value.</param>
        /// <param name="valueAngle">The angle corresponding to the current value.</param>
        /// <param name="useEasing">Indicates whether to apply an easing function.</param>
        /// <returns>The interpolated angle value.</returns>
        private double GetAdjustedAngle(DependencyObject d , double startValue , double value , double endValue , double startAngle , double endAngle , double valueAngle , bool useEasing)
        {
            // Ensure that value is within the range [startValue, endValue]
            value = Math.Max( startValue , Math.Min( endValue , value ) );

            // Calculate the interpolation factor (t) between 0 and 1
            var t = (value - startValue) / (endValue - startValue);

            double interpolatedAngle;

            if ( useEasing )
            {
                // Apply an easing function
                //var easedT = EaseInOutFunction(t);
                var easedT = EaseOutCubic(t);

                // Interpolate the angle
                interpolatedAngle = startAngle + easedT * ( endAngle - startAngle );
            }
            else
            {
                // Interpolate the angle
                interpolatedAngle = startAngle + t * ( endAngle - startAngle );
            }

            return interpolatedAngle;
        }




        /// <summary>
        /// Converts a value within a specified range to an angle within another specified range.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="minValue">The minimum value of the input range.</param>
        /// <param name="maxValue">The maximum value of the input range.</param>
        /// <param name="minAngle">The minimum angle of the output range (in degrees).</param>
        /// <param name="maxAngle">The maximum angle of the output range (in degrees).</param>
        /// <returns>The converted angle.</returns>
        private double DoubleToAngle(double value , double minValue , double maxValue , double minAngle , double maxAngle)
        {
            // If value is below the Minimum set
            if ( value < minValue )
            {
                return minAngle;
            }

            // If value is above the Maximum set
            if ( value > maxValue )
            {
                return maxAngle;
            }

            // Calculate the normalized value
            double normalizedValue = (value - minValue) / (maxValue - minValue);

            // Determine the angle range
            double angleRange = MaxAngle - MinAngle;

            // Calculate the actual angle
            double angle = MinAngle + (normalizedValue * angleRange);

            return angle;
        }




        /// <summary>
        /// Converts a value within a specified range to a percentage.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="minValue">The minimum value of the input range.</param>
        /// <param name="maxValue">The maximum value of the input range.</param>
        /// <returns>The percentage value (between 0 and 100).</returns>
        private double DoubleToPercentage(double value , double minValue , double maxValue)
        {
            // Ensure value is within the specified range
            if ( value < minValue )
            {
                return 0.0; // Below the range
            }
            else if ( value > maxValue )
            {
                return 100.0; // Above the range
            }
            else
            {
                // Calculate the normalized value
                var normalizedValue = (value - minValue) / (maxValue - minValue);

                // Convert to percentage
                var percentage = normalizedValue * 100.0;

                double roundedPercentage = Math.Round(percentage , 2, MidpointRounding.ToEven);

                return roundedPercentage;
            }
        }




        /// <summary>
        /// Converts a percentage within a specified range to a value.
        /// </summary>
        /// <param name="percentage">The percentage to convert.</param>
        /// <param name="minValue">The minimum value of the input range.</param>
        /// <param name="maxValue">The maximum value of the input range.</param>
        /// <returns>The percentage value (between 0 and 100).</returns>
        private double PercentageToValue(double percentage , double minValue , double maxValue)
        {
            double convertedValue = percentage * (maxValue - minValue) / 100.0;

            // Ensure the converted value stays within the specified range
            if ( convertedValue < minValue )
            {
                convertedValue = minValue;
            }
            else if ( convertedValue > maxValue )
            {
                convertedValue = maxValue;
            }

            return convertedValue;
        }




        /// <summary>
        /// Calculates the total angle needed to accommodate a gap between two strokes around a circle.
        /// </summary>
        /// <param name="thickness">The Thickness radius to measure.</param>
        /// <param name="radius">The radius of the rings.</param>
        /// <returns>The gap angle (sum of angles for the larger and smaller strokes).</returns>
        private double GapThicknessToAngle(double radius , double thickness)
        {
            if ( radius > 0 && thickness > 0 )
            {
                // Calculate the maximum number of circles
                double n = Math.PI * (radius / thickness);

                // Calculate the angle between each small circle
                double angle = 360.0 / n;

                return angle;
            }
            return 0;
        }




        /// <summary>
        /// Calculates an adjusted angle using linear interpolation (lerp) between the start and end angles.
        /// </summary>
        /// <param name="startAngle">The initial angle.</param>
        /// <param name="endAngle">The final angle.</param>
        /// <param name="valueAngle">A value between 0 and 1 representing the interpolation factor.</param>
        /// <returns>The adjusted angle based on linear interpolation.</returns>
        private static double GetInterpolatedAngle(double startAngle , double endAngle , double valueAngle)
        {
            // Linear interpolation formula (lerp): GetInterpolatedAngle = (startAngle + valueAngle) * (endAngle - startAngle)
            return ( startAngle + valueAngle ) * ( endAngle - startAngle );
        }




        /// <summary>
        /// Example quadratic ease-in-out function
        /// </summary>
        private double EaseInOutFunction(double t)
        {
            return t < 0.5 ? 2 * t * t : 1 - Math.Pow( -2 * t + 2 , 2 ) / 2;
        }




        /// <summary>
        /// Example ease-out cubic function
        /// </summary>
        static double EaseOutCubic(double t)
        {
            return 1.0 - Math.Pow( 1.0 - t , 3.0 );
        }




        /// <summary>
        /// Checks True if the Angle range is a Full Circle
        /// </summary>
        /// <param name="MinAngle"></param>
        /// <param name="MaxAngle"></param>
        /// <returns></returns>
        public static bool IsFullCircle(double MinAngle , double MaxAngle)
        {
            // Calculate the absolute difference between angles
            double angleDifference = Math.Abs(MaxAngle - MinAngle);

            // Check if the angle difference is equal to 360 degrees
            return angleDifference == 360;
        }

        #endregion
    }
}
