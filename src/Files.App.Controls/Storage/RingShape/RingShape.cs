// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Foundation;

namespace Files.App.Controls.Primitives
{
    /// <summary>
    /// RingShape - Primitive Path shape for drawing a
    /// circular or eliptical Ring.
    /// </summary>
    public partial class RingShape : Path
    {
        #region 1. Private Variables

        private bool				_isUpdating;				// Is True when path is updating
        private bool				_isCircle;					// When True, Width and Height are equalised
        private Size				_equalSize;                 // Calculated where Width and Height are equal
        private double				_equalRadius;               // Calculated where RadiusWidth and RadiusHeight are equal
        private Point				_centerPoint;				// Center Point within Width and Height bounds
        private double				_normalisedMinAngle;		// Normalised MinAngle between -180 and 540
        private double				_normalisedMaxAngle;        // Normalised MaxAngle between 0 and 360
        private double              _validStartAngle;           // The validated StartAngle
        private double              _validEndAngle;             // The validated EndAngle
        private double				_radiusWidth;				// The radius Width
        private double				_radiusHeight;              // The radius Height
        private SweepDirection		_sweepDirection;            // The SweepDirection

        #endregion




        #region 2. Private Setters

        /// <summary>
        /// Sets the private _isUpdating value
        /// </summary>
        /// <param name="isUpdating">The path's IsUpdating bool value</param>
        private void SetIsUpdating(bool isUpdating)
        { 
            _isUpdating = isUpdating;
        }

        /// <summary>
        /// Sets the private _isCircle value
        /// </summary>
        /// <param name="isCircle">The path's IsCircle bool value</param>
        private void SetIsCircle(bool isCircle)
        {
            _isCircle = isCircle;
        }

        /// <summary>
        /// Sets the private _equalSize value
        /// </summary>
        /// <param name="equalSize">The calculated EqualSize value</param>
        private void SetEqualSize(Size equalSize)
        {
            _equalSize = equalSize;
        }

        /// <summary>
        /// Sets the private _equalRadius value
        /// </summary>
        /// <param name="equalRadius">The calculated EqualRadius</param>
        private void SetEqualRadius(double equalRadius)
        {
            _equalRadius = equalRadius;
        }

        /// <summary>
        /// Sets the private _centerPoint value
        /// </summary>
        /// <param name="centerPoint">The calculated Center Point of the shape</param>
        private void SetCentrePoint(Point centerPoint)
        {
            _centerPoint = centerPoint;
        }

        /// <summary>
        /// Sets the private _normalisedMinAngle value
        /// </summary>
        /// <param name="normalisedMinAngle">The normalised MinAngle</param>
        private void SetNormalisedMinAngle(double normalisedMinAngle)
        {
            _normalisedMinAngle = normalisedMinAngle;
        }

        /// <summary>
        /// Sets the private _normalisedMaxAngle value
        /// </summary>
        /// <param name="normalisedMaxAngle">The normalised MaxAngle</param>
        private void SetNormalisedMaxAngle(double normalisedMaxAngle)
        {
            _normalisedMaxAngle = normalisedMaxAngle;
        }

        /// <summary>
        /// Sets the private _validStartAngle value
        /// </summary>
        /// <param name="validStartAngle">The validated StartAngle</param>
        private void SetValidStartAngle(double validStartAngle)
        {
            _validStartAngle = validStartAngle;
        }

        /// <summary>
        /// Sets the private _validEndAngle value
        /// </summary>
        /// <param name="validEndAngle">The validated EndAngle</param>
        private void SetValidEndAngle(double validEndAngle)
        {
            _validEndAngle = validEndAngle;
        }

        /// <summary>
        /// Sets the private _radiusWidth value
        /// </summary>
        /// <param name="radiusWidth">The Radius Width</param>
        private void SetRadiusWidth(double radiusWidth)
        {
            _radiusWidth = radiusWidth;
        }

        /// <summary>
        /// Sets the private _radiusHeight value
        /// </summary>
        /// <param name="radiusHeight">The Radius Height</param>
        private void SetRadiusHeight(double radiusHeight)
        {
            _radiusHeight = radiusHeight;
        }

        /// <summary>
        /// Sets the private _sweepDirection value
        /// </summary>
        /// <param name="sweepDirection">The SweepDirection</param>
        private void SetSweepDirection(SweepDirection sweepDirection)
        {
            _sweepDirection = sweepDirection;
        }

        #endregion




        #region 3. Private Getters

        /// <summary>
        /// Gets the private stored _isUpdating value
        /// </summary>
        /// <returns>The isUpdating bool value</returns>
        private bool CheckIsUpdating()
        { 
            return _isUpdating;
        }

        /// <summary>
        /// Gets the private stored _isCircle value
        /// </summary>
        /// <returns>The isCircle bool value</returns>
        private bool CheckIsCircle()
        {
            return _isCircle;
        }

        /// <summary>
        /// Gets the private stored _equalSize value
        /// </summary>
        /// <returns>the EqualSize value</returns>
        private Size GetEqualSize()
        {
            return _equalSize;
        }

        /// <summary>
        /// Gets the private stored _equalRadius value
        /// </summary>
        /// <returns>The EqualRadius</returns>
        private double GetEqualRadius()
        {
            return _equalRadius;
        }

        /// <summary>
        /// Gets the private stored _centerPoint value
        /// </summary>
        /// <returns>The Center Point</returns>
        private Point GetCenterPoint()
        {
            return _centerPoint;
        }

        /// <summary>
        /// Gets the private stored _normalisedMinAngle value
        /// </summary>
        /// <returns>The normalised MinAngle</returns>
        private double GetNormalisedMinAngle()
        {
            return _normalisedMinAngle;
        }

        /// <summary>
        /// Gets the private stored _normalisedMaxAngle value
        /// </summary>
        /// <returns>The normalised MaxAngle</returns>
        private double GetNormalisedMaxAngle()
        {
            return _normalisedMaxAngle;
        }

        /// <summary>
        /// Gets the private stored _validStartAngle value
        /// </summary>
        /// <returns>The validated StartAngle</returns>
        private double GetValidStartAngle()
        {
            return _validStartAngle;
        }

        /// <summary>
        /// Gets the private stored _validEndAngle value
        /// </summary>
        /// <returns>The validated EndAngle</returns>
        private double GetValidEndAngle()
        {
            return _validEndAngle;
        }

        /// <summary>
        /// Gets the private stored _radiusWidth value
        /// </summary>
        /// <returns>The Radius Width</returns>
        private double GetRadiusWidth()
        {
            return _radiusWidth;
        }

        /// <summary>
        /// Gets the private stored _radiusHeight value
        /// </summary>
        /// <returns>The Radius Height</returns>
        private double GetRadiusHeight()
        {
            return _radiusHeight;
        }

        /// <summary>
        /// Gets the private stored _sweepDirection value
        /// </summary>
        /// <returns>The SweepDirection</returns>
        private SweepDirection GetSweepDirection()
        {
            return _sweepDirection;
        }

        #endregion




        #region 4. Initialisation

        /// <summary>
        /// Initializes a new instance of the <see cref="RingShape" /> class.
        /// </summary>
        public RingShape()
        {
            this.SizeChanged += RingShape_SizeChanged;

            RegisterPropertyChangedCallback( StrokeThicknessProperty , OnStrokeThicknessChanged );
        }

        #endregion




        #region 5. PropertyChanged Events

        /// <summary>
        /// Invoked when the StartAngle property is changed
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="newStartAngle">The new StartAngle</param>
        private void StartAngleChanged(DependencyObject d , double newStartAngle)
        {
            RingShape ringShape = (RingShape)d;

            ringShape.BeginUpdate();

            ValidateAngle( ringShape , newStartAngle , true );

            ringShape.EndUpdate();
        }




        /// <summary>
        /// Invoked when the EndAngle property is changed
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="newEndAngle">The new EndAngle value</param>
        private void EndAngleChanged(DependencyObject d , double newEndAngle)
        {
            RingShape ringShape = (RingShape)d;

            ringShape.BeginUpdate();

            ValidateAngle( ringShape , newEndAngle, false );

            ringShape.EndUpdate();
        }




        /// <summary>
        /// Invoked when the IsCircle property is changed
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="isCircle">Bool value, True if IsCircle is set</param>
        private void IsCircleChanged(DependencyObject d, bool isCircle)
        {
            RingShape ringShape = (RingShape)d;

            ringShape.BeginUpdate();

            if ( isCircle == true )
            {
                ringShape.SetIsCircle( true );
            }
            else
            {
                ringShape.SetIsCircle( false );
            }

            ringShape.EndUpdate();
        }




        /// <summary>
        /// Invoked when the RadiusWidth property is changed
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="radiusWidth">The RadiusWidth value</param>
        private void RadiusWidthChanged(DependencyObject d, double radiusWidth)
        {
            RingShape ringShape = (RingShape)d;

            ringShape.BeginUpdate();

            AdjustRadiusWidth( ringShape , radiusWidth , ringShape.StrokeThickness );

            ringShape.EndUpdate();
        }




        /// <summary>
        /// Invoked when the RadiusHeight property is changed
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="radiusHeight">The RadiusHeight value</param>
        private void RadiusHeightChanged(DependencyObject d , double radiusHeight)
        {
            RingShape ringShape = (RingShape)d;

            ringShape.BeginUpdate();

            AdjustRadiusHeight( ringShape , radiusHeight , ringShape.StrokeThickness );

            ringShape.EndUpdate();
        }




        /// <summary>
        /// Invoked when either the element's SizeChanged event is triggered
        /// </summary>
        /// <param name="obj">The object calling the size change</param>
        /// <param name="e">The SizeChangedEventArgs</param>
        private void RingShape_SizeChanged(object obj , SizeChangedEventArgs e)
        {
            RingShape ringShape = (RingShape)obj;

            ringShape.BeginUpdate();

            ringShape.EndUpdate();
        }




        /// <summary>
        /// Invoked when the StrokeThickness property is changed
        /// </summary>
        /// <param name="d">The DependencyObject containing the StrokeThickness property</param>
        /// <param name="dp">The DependencyProperty being changed</param>
        private void OnStrokeThicknessChanged(DependencyObject d , DependencyProperty dp)
        {
            RingShape ringShape = (RingShape)d;

            ringShape.BeginUpdate();

            ringShape.EndUpdate();
        }




        /// <summary>
        /// Invoked when MinAngle or MaxAngle properties is changed
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="newAngle">The new angle value</param>
        /// <param name="isMax">True if the value changed is the MaxAngle</param>
        private void MinMaxAngleChanged(DependencyObject d , double newAngle, bool isMax)
        {
            RingShape ringShape = (RingShape)d;

            ringShape.BeginUpdate();

            if ( isMax )
            {
                CalculateAndSetNormalisedAngles( ringShape , ringShape.MinAngle , newAngle );
            }
            else
            {
                CalculateAndSetNormalisedAngles( ringShape , newAngle , ringShape.MaxAngle );
            }

            ringShape.EndUpdate();
        }




        /// <summary>
        /// Invoked as the Direction property is changed
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="newSweepDirection">The new SweepDirection that has been set</param>
        private void SweepDirectionChanged(DependencyObject d , SweepDirection newSweepDirection)
        {
            RingShape ringShape = (RingShape)d;

            ringShape.BeginUpdate();

            SetSweepDirection(newSweepDirection);

            ringShape.EndUpdate();
        }

        #endregion




        #region 6. Updates

        /// <summary>
        /// Suspends path updates until EndUpdate is called
        /// </summary>
        public void BeginUpdate()
        {
            _isUpdating = true;
        }




        /// <summary>
        /// Resumes immediate path updates every time a component property value changes. Updates the path
        /// </summary>
        public void EndUpdate()
        {
            _isUpdating = false;

            UpdatePath();
        }




        /// <summary>
        /// Updates sizes, center point and radii
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        public void UpdateSizeAndStroke(DependencyObject d)
        {
            RingShape ringShape = (RingShape)d;

            AdjustRadiusWidth( ringShape , ringShape.RadiusWidth , ringShape.StrokeThickness );
            AdjustRadiusHeight( ringShape , ringShape.RadiusHeight , ringShape.StrokeThickness );

            SetEqualSize( CalculateEqualSize( new Size( ringShape.Width , ringShape.Height ) , ringShape.StrokeThickness ) );
            SetEqualRadius( CalculateEqualRadius( ringShape , ringShape.RadiusWidth , ringShape.RadiusHeight , ringShape.StrokeThickness ) );

            SetCentrePoint( new Point( ringShape.Width / 2 , ringShape.Height / 2 ) );
            ringShape.Center = GetCenterPoint();

            CalculateAndSetNormalisedAngles( ringShape , ringShape.MinAngle , ringShape.MaxAngle );

            ValidateAngle( ringShape , ringShape.StartAngle , true );
            ValidateAngle( ringShape , ringShape.EndAngle , false );
        }




        /// <summary>
        /// Updates the RingShape path
        /// </summary>
        private void UpdatePath()
        {

            if ( _isUpdating || 
                this.ActualWidth <= 0 || this.ActualHeight <= 0 ||
                GetRadiusWidth() <= 0 || GetRadiusHeight() <= 0 )
            {
                return;
            }

            UpdateSizeAndStroke( this );

            var startAngle = GetValidStartAngle();
            var endAngle = GetValidEndAngle();

            // If the ring is closed and complete
            if ( endAngle >= startAngle + 360 )
            {
                EllipseGeometry eg;

                if ( CheckIsCircle() == true )
                {
                    eg = new EllipseGeometry
                    {
                        Center = GetCenterPoint(),
                        RadiusX = GetEqualRadius(),
                        RadiusY = GetEqualRadius(),
                    };
                }
                else
                {
                    eg = new EllipseGeometry
                    {
                        Center = GetCenterPoint(),
                        RadiusX = GetRadiusWidth(),
                        RadiusY = GetRadiusHeight(),
                    };
                }

                this.Data = eg;
            }
            else
            {
                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure();
                pathFigure.IsClosed = false;
                pathFigure.IsFilled = false;

                var center = GetCenterPoint();

                // Arc
                var ArcSegment = new ArcSegment();

                if ( CheckIsCircle() == true )
                {
                    var radius = GetEqualRadius();

                    this.ActualRadiusWidth = radius;
                    this.ActualRadiusHeight = radius;

                    // Start Point
                    // Counterclockwise
                    if ( this.SweepDirection == SweepDirection.Counterclockwise )
                    {
                        pathFigure.StartPoint =
                        new Point(
                            center.X - Math.Sin( startAngle * Math.PI / 180 ) * radius ,
                            center.Y - Math.Cos( startAngle * Math.PI / 180 ) * radius );
                    }
                    // Clockwise
                    else
                    {
                        pathFigure.StartPoint =
                            new Point(
                                center.X + Math.Sin( startAngle * Math.PI / 180 ) * radius ,
                                center.Y - Math.Cos( startAngle * Math.PI / 180 ) * radius );
                    }


                    // End Point
                    // Counterclockwise
                    if ( this.SweepDirection == SweepDirection.Counterclockwise )
                    {
                        ArcSegment.Point =
                            new Point(
                                center.X - Math.Sin( endAngle * Math.PI / 180 ) * radius ,
                                center.Y - Math.Cos( endAngle * Math.PI / 180 ) * radius );
                        
                        if ( endAngle < startAngle )
                        {
                            ArcSegment.IsLargeArc = ( endAngle - startAngle ) <= -180.0;
                            ArcSegment.SweepDirection = SweepDirection.Clockwise;
                        }
                        else
                        {
                            ArcSegment.IsLargeArc = ( endAngle - startAngle ) >= 180.0;
                            ArcSegment.SweepDirection = SweepDirection.Counterclockwise;
                        }
                    }
                    // Clockwise
                    else
                    {
                        ArcSegment.Point =
                            new Point(
                                center.X + Math.Sin( endAngle * Math.PI / 180 ) * radius ,
                                center.Y - Math.Cos( endAngle * Math.PI / 180 ) * radius );
                        //ArcSegment.IsLargeArc = ( endAngle - startAngle ) >= 180.0;
                        if ( endAngle < startAngle )
                        {
                            ArcSegment.IsLargeArc = ( endAngle - startAngle ) <= -180.0;
                            ArcSegment.SweepDirection = SweepDirection.Counterclockwise;
                        }
                        else
                        {
                            ArcSegment.IsLargeArc = ( endAngle - startAngle ) >= 180.0;
                            ArcSegment.SweepDirection = SweepDirection.Clockwise;
                        }
                    }
                    ArcSegment.Size = new Size( radius , radius );
                }
                else
                {
                    var radiusWidth = GetRadiusWidth();
                    var radiusHeight = GetRadiusHeight();

                    this.ActualRadiusWidth = radiusWidth;
                    this.ActualRadiusHeight = radiusHeight;

                    // Start Point
                    // Counterclockwise
                    if ( this.SweepDirection == SweepDirection.Counterclockwise )
                    {
                        pathFigure.StartPoint =
                        new Point(
                            center.X - Math.Sin( startAngle * Math.PI / 180 ) * radiusWidth ,
                            center.Y - Math.Cos( startAngle * Math.PI / 180 ) * radiusHeight );
                    }
                    // Clockwise
                    else
                    {
                        pathFigure.StartPoint =
                        new Point(
                            center.X + Math.Sin( startAngle * Math.PI / 180 ) * radiusWidth ,
                            center.Y - Math.Cos( startAngle * Math.PI / 180 ) * radiusHeight );
                    }					


                    // EndPoint
                    // Counterclockwise
                    if ( this.SweepDirection == SweepDirection.Counterclockwise )
                    {
                        ArcSegment.Point =
                            new Point(
                                center.X - Math.Sin( endAngle * Math.PI / 180 ) * radiusWidth ,
                                center.Y - Math.Cos( endAngle * Math.PI / 180 ) * radiusHeight );
                        //ArcSegment.IsLargeArc = ( endAngle - startAngle ) >= 180.0;
                        if ( endAngle < startAngle )
                        {
                            ArcSegment.IsLargeArc = ( endAngle - startAngle ) >= 180.0;
                            ArcSegment.SweepDirection = SweepDirection.Clockwise;
                        }
                        else
                        {
                            ArcSegment.IsLargeArc = ( endAngle - startAngle ) >= 180.0;
                            ArcSegment.SweepDirection = SweepDirection.Counterclockwise;
                        }
                    }
                    // Clockwise
                    else
                    {
                        ArcSegment.Point =
                            new Point(
                                center.X + Math.Sin( endAngle * Math.PI / 180 ) * radiusWidth ,
                                center.Y - Math.Cos( endAngle * Math.PI / 180 ) * radiusHeight );
                        //ArcSegment.IsLargeArc = ( endAngle - startAngle ) >= 180.0;
                        if ( endAngle < startAngle )
                        {
                            ArcSegment.IsLargeArc = ( endAngle - startAngle ) >= 180.0;
                            ArcSegment.SweepDirection = SweepDirection.Counterclockwise;
                        }
                        else
                        {
                            ArcSegment.IsLargeArc = ( endAngle - startAngle ) >= 180.0;
                            ArcSegment.SweepDirection = SweepDirection.Clockwise;
                        }
                    }
                    ArcSegment.Size = new Size( radiusWidth , radiusHeight );
                }

                pathFigure.Segments.Add( ArcSegment );
                pathGeometry.Figures.Add( pathFigure );
                this.InvalidateArrange();
                this.Data = pathGeometry;
            }
        }

        #endregion




        #region 7. Value Calculations

        /// <summary>
        /// Calculates the EqualSize taking the smaller of the given Size's
        /// Width and Height
        /// </summary>
        /// <param name="size">The Size we want to use for calculating</param>
        /// <param name="strokeThickness">The StrokeThickness value</param>
        /// <returns>The calculated EqualisedSize</returns>
        private static Size CalculateEqualSize( Size size , double strokeThickness)
        { 
            double adjWidth  = size.Width;
            double adjHeight = size.Height;

            var smaller = Math.Min(adjWidth, adjHeight);

            if ( smaller > strokeThickness * 2 )
            {
                return new Size( smaller , smaller );
            }
            else
            {
                return new Size( strokeThickness * 2 , strokeThickness * 2 );
            }
        }




        /// <summary>
        /// Calculates the smaller of the RadiusWidth and RadiusHeight when both
        /// should be the same value.
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="radiusWidth">The RadiusWidth value</param>
        /// <param name="radiusHeight">The RadiusHeight value</param>
        /// <param name="strokeThickness">The StrokeThickness value</param>
        /// <returns>The calculated EqualRadius</returns>
        private static double CalculateEqualRadius(DependencyObject d , double radiusWidth , double radiusHeight , double strokeThickness)
        {
            RingShape ringShape = (RingShape)d;

            double adjWidth  = radiusWidth;
            double adjHeight = radiusHeight;

            var smaller = Math.Min( adjWidth , adjHeight );

            if ( smaller <= strokeThickness )
            {
                return strokeThickness;
            }
            else if ( smaller >= ( ( ringShape.GetEqualSize().Width / 2 ) - (strokeThickness / 2 ) ) )
            {
                return ( ringShape.GetEqualSize().Width / 2 ) - ( strokeThickness / 2 );
            }
            else
            {
                return smaller;
            }
        }




        /// <summary>
        /// Calculates the center point based on half the Width and Height
        /// </summary>
        /// <param name="Width">The Width value</param>
        /// <param name="Height">The Height value</param>
        /// <returns>The calculated Center Point</returns>
        private static Point CalculateCenter( double Width , double Height )
        {
            Point calculatedCenter = new Point ( ( Width / 2.0 ) , ( Height / 2.0 ) );

            return calculatedCenter;
        }




        /// <summary>
        /// Calculates and Sets the normalised Min and Max Angles
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="minAngle">MinAngle in the range from -180 to 180.</param>
        /// <param name="maxAngle">MaxAngle, in the range from -180 to 540.</param>
        private static void CalculateAndSetNormalisedAngles( DependencyObject d , double minAngle , double maxAngle )
        {
            RingShape ringShape = (RingShape)d;

            var result = CalculateModulus( minAngle , 360 );

            if ( result >= 180 )
            {
                result = result - 360;
            }

            ringShape.SetNormalisedMinAngle( result );

            result = CalculateModulus( maxAngle , 360 );

            if ( result < 180 )
            {
                result = result + 360;
            }

            if ( result > ringShape.GetNormalisedMinAngle() + 360 )
            {
                result = result - 360;
            }


            ringShape.SetNormalisedMaxAngle( result );
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
        /// 
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="angle">The Angle to validate</param>
        /// <param name="isStart">Bool to check if we are validating Start or End angle</param>
        private void ValidateAngle(DependencyObject d , double angle , bool isStart)
        {
            RingShape ringShape = (RingShape)d;

            if ( angle >= GetNormalisedMaxAngle() )
            {
                if ( isStart == true )
                {
                    SetValidStartAngle( GetNormalisedMaxAngle() );
                }
                else
                {
                    SetValidEndAngle( GetNormalisedMaxAngle() );
                }
            }
            else if ( angle <= GetNormalisedMinAngle() )
            {
                if ( isStart == true )
                {
                    SetValidStartAngle( GetNormalisedMinAngle() );
                }
                else
                {
                    SetValidEndAngle( GetNormalisedMinAngle() );
                }
            }
            else
            {
                if ( isStart == true )
                {
                    SetValidStartAngle( angle );
                }
                else
                {
                    SetValidEndAngle( angle );
                }
            }
        }




        /// <summary>
        /// Adjust the RadiusWidth to fit within the bounds
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="radiusWidth">The RadiusWidth to adjust</param>
        /// <param name="strokeThickness">The Stroke Thickness</param>
        private void AdjustRadiusWidth(DependencyObject d , double radiusWidth , double strokeThickness)
        {
            RingShape ringShape = (RingShape)d;

            var maxValue = ( ringShape.Width / 2 ) - ( ringShape.StrokeThickness / 2 );
            var threshold = strokeThickness;

            if ( radiusWidth >= maxValue )
            {
                ringShape.SetRadiusWidth( maxValue );
            }
            else if ( radiusWidth <= maxValue && radiusWidth >= threshold )
            {
                ringShape.SetRadiusWidth( radiusWidth );
            }
            else
            {
                ringShape.SetRadiusWidth( threshold );
            }
        }




        /// <summary>
        /// Adjust the RadiusHeight to fit within the bounds
        /// </summary>
        /// <param name="d">The DependencyObject calling the function</param>
        /// <param name="radiusHeight">The RadiusHeight to adjust</param>
        /// <param name="strokeThickness">The Stroke Thickness</param>
        private void AdjustRadiusHeight(DependencyObject d , double radiusHeight , double strokeThickness)
        {
            RingShape ringShape = (RingShape)d;

            var maxValue = ( ringShape.Height / 2 ) - ( ringShape.StrokeThickness / 2 );
            var threshold = strokeThickness;

            if ( radiusHeight >= maxValue )
            {
                ringShape.SetRadiusHeight( maxValue );
            }
            else if ( radiusHeight <= maxValue && radiusHeight >= threshold )
            {
                ringShape.SetRadiusHeight( radiusHeight );
            }
            else
            {
                ringShape.SetRadiusHeight( threshold );
            }
        }

        #endregion
    }
}
