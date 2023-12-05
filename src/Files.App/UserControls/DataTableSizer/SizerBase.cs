using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Specialized;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls.DataTableSizer
{
	[TemplateVisualState(Name = NormalState, GroupName = CommonStates)]
	[TemplateVisualState(Name = PointerOverState, GroupName = CommonStates)]
	[TemplateVisualState(Name = PressedState, GroupName = CommonStates)]
	[TemplateVisualState(Name = DisabledState, GroupName = CommonStates)]
	[TemplateVisualState(Name = HorizontalState, GroupName = OrientationStates)]
	[TemplateVisualState(Name = VerticalState, GroupName = OrientationStates)]
	[TemplateVisualState(Name = VisibleState, GroupName = ThumbVisibilityStates)]
	[TemplateVisualState(Name = CollapsedState, GroupName = ThumbVisibilityStates)]
	public abstract partial class SizerBase : Control
	{
		internal const string CommonStates = "CommonStates";
		internal const string NormalState = "Normal";
		internal const string PointerOverState = "PointerOver";
		internal const string PressedState = "Pressed";
		internal const string DisabledState = "Disabled";
		internal const string OrientationStates = "OrientationStates";
		internal const string HorizontalState = "Horizontal";
		internal const string VerticalState = "Vertical";
		internal const string ThumbVisibilityStates = "ThumbVisibilityStates";
		internal const string VisibleState = "Visible";
		internal const string CollapsedState = "Collapsed";

		/// <summary>
		/// Called when the control has been initialized.
		/// </summary>
		/// <param name="e">Loaded event args.</param>
		protected virtual void OnLoaded(RoutedEventArgs e)
		{
		}

		/// <summary>
		/// Called when the <see cref="SizerBase"/> control starts to be dragged by the user.
		/// Implementer should record current state of manipulated target at this point in time.
		/// They will receive the cumulative change in <see cref="OnDragHorizontal(double)"/> or
		/// <see cref="OnDragVertical(double)"/> based on the <see cref="Orientation"/> property.
		/// </summary>
		/// <remarks>
		/// This method is also called at the start of a keyboard interaction. Keyboard strokes use the same pattern to emulate a mouse movement for a single change. The appropriate
		/// <see cref="OnDragHorizontal(double)"/> or <see cref="OnDragVertical(double)"/>
		/// method will also be called after when the keyboard is used.
		/// </remarks>
		protected abstract void OnDragStarting();

		/// <summary>
		/// Method to process the requested horizontal resize.
		/// </summary>
		/// <param name="horizontalChange">The <see cref="ManipulationDeltaRoutedEventArgs.Cumulative"/> horizontal change amount from the start in device-independent pixels DIP.</param>
		/// <returns><see cref="bool"/> indicates if a change was made</returns>
		/// <remarks>
		/// The value provided here is the cumulative change from the beginning of the
		/// manipulation. This method will be used regardless of input device. It will already
		/// be adjusted for RightToLeft <see cref="FlowDirection"/> of the containing
		/// layout/settings. It will also already account for any settings such as
		/// <see cref="DragIncrement"/> or <see cref="KeyboardIncrement"/>. The implementer
		/// just needs to use the provided value to manipulate their baseline stored
		/// in <see cref="OnDragStarting"/> to provide the desired change.
		/// </remarks>
		protected abstract bool OnDragHorizontal(double horizontalChange);

		/// <summary>
		/// Method to process the requested vertical resize.
		/// </summary>
		/// <param name="verticalChange">The <see cref="ManipulationDeltaRoutedEventArgs.Cumulative"/> vertical change amount from the start in device-independent pixels DIP.</param>
		/// <returns><see cref="bool"/> indicates if a change was made</returns>
		/// <remarks>
		/// The value provided here is the cumulative change from the beginning of the
		/// manipulation. This method will be used regardless of input device. It will also
		/// already account for any settings such as <see cref="DragIncrement"/> or
		/// <see cref="KeyboardIncrement"/>. The implementer just needs
		/// to use the provided value to manipulate their baseline stored
		/// in <see cref="OnDragStarting"/> to provide the desired change.
		/// </remarks>
		protected abstract bool OnDragVertical(double verticalChange);

		/// <summary>
		/// Initializes a new instance of the <see cref="SizerBase"/> class.
		/// </summary>
		public SizerBase()
		{
			this.DefaultStyleKey = typeof(SizerBase);
		}

		/// <summary>
		/// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
		/// </summary>
		/// <returns>An automation peer for this <see cref="SizerBase"/>.</returns>
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new SizerAutomationPeer(this);
		}

		// On Uno the ProtectedCursor isn't supported yet, so we don't need this value.
		// Used to track when we're in the OnApplyTemplateStep to change ProtectedCursor value.
		private bool _appliedTemplate = false;

		/// <inheritdoc/>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Unregister Events
			Loaded -= SizerBase_Loaded;
			PointerEntered -= SizerBase_PointerEntered;
			PointerExited -= SizerBase_PointerExited;
			PointerPressed -= SizerBase_PointerPressed;
			PointerReleased -= SizerBase_PointerReleased;
			ManipulationStarted -= SizerBase_ManipulationStarted;
			ManipulationCompleted -= SizerBase_ManipulationCompleted;
			IsEnabledChanged -= SizerBase_IsEnabledChanged;

			// Register Events
			Loaded += SizerBase_Loaded;
			PointerEntered += SizerBase_PointerEntered;
			PointerExited += SizerBase_PointerExited;
			PointerPressed += SizerBase_PointerPressed;
			PointerReleased += SizerBase_PointerReleased;
			ManipulationStarted += SizerBase_ManipulationStarted;
			ManipulationCompleted += SizerBase_ManipulationCompleted;
			IsEnabledChanged += SizerBase_IsEnabledChanged;

			// Trigger initial state transition based on if we're Enabled or not currently.
			SizerBase_IsEnabledChanged(this, null!);

			// On WinAppSDK, we'll trigger this to setup the initial ProtectedCursor value.
			_appliedTemplate = true;

			// Ensure we have the proper cursor value setup, as we can only set now for WinUI 3
			OnOrientationPropertyChanged(this, null!);

			// Ensure we set the Thumb visibility
			OnIsThumbVisiblePropertyChanged(this, null!);
		}

		private void SizerBase_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= SizerBase_Loaded;

			OnLoaded(e);
		}
	}
}
