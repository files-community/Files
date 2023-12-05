using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Specialized;
using Windows.ApplicationModel.DataTransfer;
using CursorEnum = Microsoft.UI.Input.InputSystemCursorShape;

namespace Files.App.UserControls.DataTableSizer
{
	public partial class ContentSizer
	{
		/// <summary>
		/// Gets or sets a value indicating whether the <see cref="ContentSizer"/> control is resizing in the opposite direction.
		/// </summary>
		public bool IsDragInverted
		{
			get { return (bool)GetValue(IsDragInvertedProperty); }
			set { SetValue(IsDragInvertedProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="IsDragInverted"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty IsDragInvertedProperty =
			DependencyProperty.Register(nameof(IsDragInverted), typeof(bool), typeof(ContentSizer), new PropertyMetadata(false));

		/// <summary>
		/// Gets or sets the control that the <see cref="ContentSizer"/> is resizing. Be default, this will be the visual ancestor of the <see cref="ContentSizer"/>.
		/// </summary>
		public FrameworkElement? TargetControl
		{
			get { return (FrameworkElement?)GetValue(TargetControlProperty); }
			set { SetValue(TargetControlProperty, value); }
		}

		/// <summary>
		/// Identifies the <see cref="TargetControl"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty TargetControlProperty =
			DependencyProperty.Register(nameof(TargetControl), typeof(FrameworkElement), typeof(ContentSizer), new PropertyMetadata(null));
	}
}
