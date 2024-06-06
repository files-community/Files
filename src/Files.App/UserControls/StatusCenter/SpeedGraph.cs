using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System.Collections.Specialized;
using System.Numerics;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.App.UserControls.StatusCenter
{
	public sealed partial class SpeedGraph : Control
	{
		public ObservableCollection<Vector2> Points
		{
			get => (ObservableCollection<Vector2>)GetValue(PointsProperty);
			set => SetValue(PointsProperty, value);
		}
		
		public static readonly DependencyProperty PointsProperty =
			DependencyProperty.Register(nameof(Points), typeof(ObservableCollection<Vector2>), typeof(SpeedGraph), null);

		Compositor compositor;

		ContainerVisual rootVisual;

		CompositionSpriteShape graphShape;
		InsetClip graphClip;

		SpriteVisual line;

		bool initialized;

		float width;
		float height;

		float highestValue;
		
		public SpeedGraph()
		{ 
			// TODO: unhook
			this.SizeChanged += OnSizeChanged;
			this.Unloaded += UserControl_Unloaded;
			this.ActualThemeChanged += UserControl_ActualThemeChanged;
		}

		private void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (initialized)
				return;

			Init();

			// added *after* first load
			Points.CollectionChanged += PointsChanged;
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (Points.Count > 0)
				UpdateGraph();

			Points.CollectionChanged += PointsChanged;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			Points.CollectionChanged -= PointsChanged;
		}

		private void PointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateGraph();
		}

		private void Init()
		{
			compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
			rootVisual = compositor.CreateContainerVisual();
			rootVisual.Size = this.ActualSize;
			ElementCompositionPreview.SetElementChildVisual(this, rootVisual);

			width = rootVisual.Size.X;
			height = rootVisual.Size.Y;

			var accentColor = (App.Current.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush)!.Color;

			var backgroundBrush = compositor.CreateColorBrush(accentColor with { A = 0x15 });

			var graphFillBrush = compositor.CreateLinearGradientBrush();
			graphFillBrush.StartPoint = new(0.5f, 0f);
			graphFillBrush.EndPoint = new(0.5f, 1f);
			graphFillBrush.ColorStops.Add(compositor.CreateColorGradientStop(0f, accentColor with { A = 0x70 }));
			graphFillBrush.ColorStops.Add(compositor.CreateColorGradientStop(1f, accentColor with { A = 0x06 }));

			var graphStrokeBrush = compositor.CreateColorBrush(accentColor);

			var bgVisual = compositor.CreateSpriteVisual();
			bgVisual.Size = rootVisual.Size;
			bgVisual.Brush = backgroundBrush;

			var graphVisual = compositor.CreateShapeVisual();
			graphVisual.Size = rootVisual.Size;
			graphShape = compositor.CreateSpriteShape();
			graphShape.FillBrush = graphFillBrush;
			graphShape.StrokeBrush = graphStrokeBrush;
			graphShape.StrokeThickness = 1f;
			graphVisual.Shapes.Add(graphShape);

			var container = compositor.CreateContainerVisual();
			container.Size = rootVisual.Size;
			container.Children.InsertAtBottom(bgVisual);
			container.Children.InsertAtBottom(graphVisual);

			graphClip = compositor.CreateInsetClip();
			graphClip.RightInset = width;
			container.Clip = graphClip;

			rootVisual.Children.InsertAtBottom(container);

			line = compositor.CreateSpriteVisual();
			line.Size = new(width, 1.5f);
			line.Brush = graphStrokeBrush;
			rootVisual.Children.InsertAtTop(line);

			highestValue = 0;

			initialized = true;
		}

		float YValue(float y) => height - (y / highestValue) * (height - 40f) - 4;

		void UpdateGraph()
		{
			var geometry = CreatePathFromPoints();
			graphShape.Geometry = geometry;

			using var lineAnim = compositor.CreateScalarKeyFrameAnimation();
			lineAnim.InsertExpressionKeyFrame(0f, "this.StartingValue");
			lineAnim.InsertKeyFrame(1f, YValue(Points[^1].Y), compositor.CreateLinearEasingFunction());
			lineAnim.Duration = TimeSpan.FromMilliseconds(72);
			line.StartAnimation("Offset.Y", lineAnim);

			using var clipAnim = compositor.CreateScalarKeyFrameAnimation();
			clipAnim.InsertExpressionKeyFrame(0f, "this.StartingValue");
			clipAnim.InsertKeyFrame(1f, width - (width * Points[^1].X / 100f) - 1, compositor.CreateLinearEasingFunction());
			clipAnim.Duration = TimeSpan.FromMilliseconds(72);
			graphClip.StartAnimation("RightInset", clipAnim);
		}

		CompositionPathGeometry CreatePathFromPoints()
		{
			using var pathBuilder = new CanvasPathBuilder(null);
			pathBuilder.BeginFigure(0f, height);
			for (int i = 0; i < Points.Count; i++)
			{
				if (Points[i].Y > highestValue)
					highestValue = Points[i].Y;
				// no smooth curve for now. a little ugly but maybe for the best performance-wise, we'll see before this gets merged
				pathBuilder.AddLine(width * Points[i].X / 100f, YValue(Points[i].Y));
			}
			// little extra part so that steep lines don't get cut off
			pathBuilder.AddLine(width * Points[^1].X / 100f + 2, YValue(Points[^1].Y));
			pathBuilder.AddLine(width * Points[^1].X / 100f + 2, height);
			pathBuilder.EndFigure(CanvasFigureLoop.Closed);
			var geometry = compositor.CreatePathGeometry();
			geometry.Path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
			return geometry;
		}
		private void UserControl_ActualThemeChanged(FrameworkElement sender, object args)
		{

		}
	}
}
