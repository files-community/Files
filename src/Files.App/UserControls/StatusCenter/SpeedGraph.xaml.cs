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
	public sealed partial class SpeedGraph : UserControl
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
		ShapeVisual graphVisual;
		CompositionSpriteShape graphShape;
		SpriteVisual line;
		InsetClip graphClip;

		float width;
		float height;

		float highestValue;
		
		public SpeedGraph()
		{ 
			this.InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			// is it a bad idea to recreate these on every load?
			// TODO: it doesn't work the first time you open the flyout
			var temp = ElementCompositionPreview.GetElementVisual(this);
			compositor = temp.Compositor;
			rootVisual = compositor.CreateContainerVisual();
			rootVisual.Size = temp.Size;
			ElementCompositionPreview.SetElementChildVisual(this, rootVisual);

			width = rootVisual.Size.X;
			height = rootVisual.Size.Y;

			var accentColor = (App.Current.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush)!.Color;

			var bgVisual = compositor.CreateSpriteVisual();
			bgVisual.Size = rootVisual.Size;
			bgVisual.Brush = compositor.CreateColorBrush(Color.FromArgb(0x15, accentColor.R, accentColor.G, accentColor.B));

			graphVisual = compositor.CreateShapeVisual();
			graphVisual.Size = rootVisual.Size;

			graphShape = compositor.CreateSpriteShape();
			var gradientFill = compositor.CreateLinearGradientBrush();
			gradientFill.StartPoint = new(0.5f, 0f);
			gradientFill.EndPoint = new(0.5f, 1f);
			gradientFill.ColorStops.Add(compositor.CreateColorGradientStop(0f, Color.FromArgb(0x70, accentColor.R, accentColor.G, accentColor.B)));
			gradientFill.ColorStops.Add(compositor.CreateColorGradientStop(1f, Color.FromArgb(0x06, accentColor.R, accentColor.G, accentColor.B)));
			graphShape.FillBrush = gradientFill;
			graphShape.StrokeBrush = compositor.CreateColorBrush(accentColor);
			graphShape.StrokeThickness = 1f;
			graphVisual.Shapes.Add(graphShape);

			var container = compositor.CreateContainerVisual();
			container.Size = rootVisual.Size;
			container.Children.InsertAtBottom(bgVisual);
			container.Children.InsertAtBottom(graphVisual);
			rootVisual.Children.InsertAtBottom(container);

			graphClip = compositor.CreateInsetClip();
			graphClip.RightInset = width;
			container.Clip = graphClip;

			line = compositor.CreateSpriteVisual();
			line.Size = new Vector2(width, 1.5f);
			line.Brush = compositor.CreateColorBrush(accentColor);
			rootVisual.Children.InsertAtTop(line);

			// if it gets unloaded and reloaded because of the flyout closing
			if (Points.Count > 0)
				UpdateGraph();

			Points.CollectionChanged += PointsChanged;
		}

		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			rootVisual.Children.RemoveAll();
			graphVisual = null!;
			graphShape = null!;
			graphClip = null!;
			Points.CollectionChanged -= PointsChanged;
		}

		private void PointsChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateGraph();
		}

		void UpdateGraph()
		{
			var geometry = CreatePathFromPoints();
			graphShape.Geometry = geometry;

			var lineAnim = compositor.CreateScalarKeyFrameAnimation();
			lineAnim.InsertExpressionKeyFrame(0f, "this.StartingValue");
			lineAnim.InsertKeyFrame(1f, height - (Points[^1].Y / highestValue) * (height - 40f) - 4, compositor.CreateLinearEasingFunction());
			lineAnim.Duration = TimeSpan.FromMilliseconds(72);
			line.StartAnimation("Offset.Y", lineAnim);

			var clipAnim = compositor.CreateScalarKeyFrameAnimation();
			clipAnim.InsertExpressionKeyFrame(0f, "this.StartingValue");
			clipAnim.InsertKeyFrame(1f, width - (width * Points[^1].X / 100f) - 2, compositor.CreateLinearEasingFunction());
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
				pathBuilder.AddLine(width * Points[i].X / 100f, height - (Points[i].Y / highestValue) * (height - 40f) - 4);
			}
			// little extra part so that steep lines don't get cut off
			pathBuilder.AddLine(width * Points[^1].X / 100f + 3, height - (Points[^1].Y / highestValue) * (height - 40f) - 4);
			pathBuilder.AddLine(width * Points[^1].X / 100f + 3, height);
			pathBuilder.EndFigure(CanvasFigureLoop.Closed);
			var geometry = compositor.CreatePathGeometry();
			geometry.Path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
			return geometry;
		}
	}
}
