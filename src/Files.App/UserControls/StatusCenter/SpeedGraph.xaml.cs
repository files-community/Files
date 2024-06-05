using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
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
			rootVisual = (ContainerVisual)ElementCompositionPreview.GetElementVisual(graphRoot);
			compositor = rootVisual.Compositor;

			width = rootVisual.Size.X;
			height = rootVisual.Size.Y;

			var bgVisual = compositor.CreateSpriteVisual();
			bgVisual.Size = rootVisual.Size;
			bgVisual.Brush = compositor.CreateColorBrush(Color.FromArgb(0x20, 0x40, 0xE0, 0xD0));
			rootVisual.Children.InsertAtBottom(bgVisual);

			graphVisual = compositor.CreateShapeVisual();
			graphVisual.Size = rootVisual.Size;
			rootVisual.Children.InsertAtBottom(graphVisual);

			graphShape = compositor.CreateSpriteShape();
			// TODO: use accent theme resources
			graphShape.FillBrush = compositor.CreateColorBrush(Colors.Transparent);
			graphShape.StrokeBrush = compositor.CreateColorBrush(Colors.Turquoise);
			graphVisual.Shapes.Add(graphShape);

			graphClip = compositor.CreateInsetClip();
			graphClip.RightInset = width;
			rootVisual.Clip = graphClip;

			//// if it gets unloaded and reloaded because of the flyout closing
			//if (Points.Count > 1)
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
			if (e.Action != NotifyCollectionChangedAction.Add)
				return;

			if (Points[^1].Y > highestValue)
				highestValue = Points[^1].Y;

			UpdateGraph();
		}

		void UpdateGraph()
		{
			var geometry = CreatePathFromPoints();
			graphShape.Geometry = geometry;
			graphClip.RightInset = width - (width * Points[^1].X / 100f) + 1;
		}

		CompositionPathGeometry CreatePathFromPoints()
		{
			using var pathBuilder = new CanvasPathBuilder(null);
			pathBuilder.BeginFigure(0f, height);
			for (int i = 0; i < Points.Count; i++)
			{
				// no smooth curve for now. a little ugly but maybe for the best performance-wise, we'll see before this gets merged
				pathBuilder.AddLine(width * Points[i].X / 100f, height - (Points[i].Y / highestValue) * (height * 0.9f));
			}
			pathBuilder.AddLine(width * Points[^1].X / 100f, height);
			pathBuilder.EndFigure(CanvasFigureLoop.Closed);
			var geometry = compositor.CreatePathGeometry();
			geometry.Path = new CompositionPath(CanvasGeometry.CreatePath(pathBuilder));
			return geometry;
		}
	}
}
