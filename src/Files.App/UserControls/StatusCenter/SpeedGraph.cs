using Microsoft.Graphics.Canvas.Geometry;
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
			set
			{
				highestValue = 0;
				SetValue(PointsProperty, value);
			}
		}
		
		public static readonly DependencyProperty PointsProperty =
			DependencyProperty.Register(nameof(Points), typeof(ObservableCollection<Vector2>), typeof(SpeedGraph), null);

		Compositor compositor;

		ContainerVisual rootVisual;

		CompositionPathGeometry graphGeometry;
		InsetClip graphClip;

		SpriteVisual line;

		CompositionColorBrush backgroundBrush;
		CompositionColorGradientStop graphFillBottom;
		CompositionColorGradientStop graphFillTop;
		CompositionColorBrush graphStrokeBrush;

		LinearEasingFunction linearEasing;

		bool initialized;

		float width;
		float height;

		float highestValue;

		IAppThemeModeService themeModeService;
		
		public SpeedGraph()
		{
			compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

			themeModeService = Ioc.Default.GetRequiredService<IAppThemeModeService>();

			this.SizeChanged += OnSizeChanged;
		}

		private void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (initialized)
				return;

			InitGraph();

			this.SizeChanged -= OnSizeChanged;

			// added *after* first load
			this.Loaded += OnLoaded;
			this.Unloaded += OnUnloaded;
			Points.CollectionChanged += OnPointsChanged;
			this.ActualThemeChanged += OnThemeChanged;
			themeModeService.AppThemeModeChanged += OnAppThemeModeChanged;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (Points.Count > 0)
			{
				SetGraphColors();
				UpdateGraph();
			}

			this.Unloaded += OnUnloaded;
			Points.CollectionChanged += OnPointsChanged;
			this.ActualThemeChanged += OnThemeChanged;
			themeModeService.AppThemeModeChanged += OnAppThemeModeChanged;
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			this.Unloaded -= OnUnloaded;
			Points.CollectionChanged -= OnPointsChanged;
			this.ActualThemeChanged -= OnThemeChanged;
			themeModeService.AppThemeModeChanged -= OnAppThemeModeChanged;
		}

		private void OnPointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateGraph();
		}

		private void OnAppThemeModeChanged(object? sender, EventArgs e)
		{
			// this seemingly doesn't fire? leaving it here in case it does in the future (if it's ever used outside of the flyout)
			if (initialized)
				SetGraphColors();
		}

		private void OnThemeChanged(FrameworkElement sender, object args)
		{
			if (initialized)
				SetGraphColors();
		}

		private void InitGraph()
		{
			rootVisual = compositor.CreateContainerVisual();
			rootVisual.Size = this.ActualSize;
			ElementCompositionPreview.SetElementChildVisual(this, rootVisual);

			var size = rootVisual.Size;
			width = size.X;
			height = size.Y;

			var rootClip = compositor.CreateRectangleClip();
			rootClip.Top = 1.5f;
			rootClip.Left = 1.5f;
			rootClip.Bottom = height - 1.5f;
			rootClip.Right = width - 2f;
			rootClip.TopLeftRadius = new(4f);
			rootClip.TopRightRadius = new(4f);
			rootClip.BottomLeftRadius = new(4f);
			rootClip.BottomRightRadius = new(4f);
			rootVisual.Clip = rootClip;

			backgroundBrush = compositor.CreateColorBrush();

			var graphFillBrush = compositor.CreateLinearGradientBrush();
			graphFillBrush.StartPoint = new(0.5f, 0f);
			graphFillBrush.EndPoint = new(0.5f, 1f);
			graphFillTop = compositor.CreateColorGradientStop();
			graphFillTop.Offset = 0f;
			graphFillBottom = compositor.CreateColorGradientStop();
			graphFillBottom.Offset = 1f;
			graphFillBrush.ColorStops.Add(graphFillBottom);
			graphFillBrush.ColorStops.Add(graphFillTop);

			graphStrokeBrush = compositor.CreateColorBrush();

			SetGraphColors();

			var container = compositor.CreateSpriteVisual();
			container.Size = rootVisual.Size;
			// container is also the graph background
			container.Brush = backgroundBrush;

			var graphVisual = compositor.CreateShapeVisual();
			graphVisual.Size = rootVisual.Size;
			var graphShape = compositor.CreateSpriteShape();
			graphShape.FillBrush = graphFillBrush;
			graphShape.StrokeBrush = graphStrokeBrush;
			graphShape.StrokeThickness = 1f;

			graphGeometry = compositor.CreatePathGeometry();
			graphShape.Geometry = graphGeometry;

			graphVisual.Shapes.Add(graphShape);

			container.Children.InsertAtBottom(graphVisual);

			graphClip = compositor.CreateInsetClip();
			graphClip.RightInset = width;
			container.Clip = graphClip;

			rootVisual.Children.InsertAtBottom(container);

			line = compositor.CreateSpriteVisual();
			line.Size = new(width, 1.5f);
			line.Brush = graphStrokeBrush;
			line.Offset = new(0f, height - 4f, 0);
			rootVisual.Children.InsertAtTop(line);

			highestValue = 0;

			linearEasing = compositor.CreateLinearEasingFunction();

			initialized = true;
		}

		float YValue(float y) => height - (y / highestValue) * (height - 48f) - 4;

		void UpdateGraph()
		{
			var path = CreatePathFromPoints();
			graphGeometry.Path = path;

			using var lineAnim = compositor.CreateScalarKeyFrameAnimation();
			lineAnim.InsertKeyFrame(1f, YValue(Points[^1].Y), linearEasing);
			lineAnim.Duration = TimeSpan.FromMilliseconds(72);
			line.StartAnimation("Offset.Y", lineAnim);

			using var clipAnim = compositor.CreateScalarKeyFrameAnimation();
			clipAnim.InsertKeyFrame(1f, width - (width * Points[^1].X / 100f) - 1, linearEasing);
			clipAnim.Duration = TimeSpan.FromMilliseconds(72);
			graphClip.StartAnimation("RightInset", clipAnim);
		}

		CompositionPath CreatePathFromPoints()
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
			var path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
			return path;
		}

		void SetGraphColors()
		{
			var accentColor = themeModeService.DefaultAccentColor;

			var veryLightColor = accentColor with { A = 0x0f };

			var slightlyDarkerColor = this.ActualTheme switch
			{
				ElementTheme.Light => accentColor with { A = 0x55 },
				_ => accentColor with { A = 0x7f }
			};

			backgroundBrush.Color = veryLightColor;

			graphFillTop.Color = slightlyDarkerColor;
			graphFillBottom.Color = veryLightColor;

			graphStrokeBrush.Color = accentColor;
		}
	}
}
