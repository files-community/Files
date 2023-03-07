using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Settings
{
	public partial class SettingsCard : ButtonBase
	{
		private const string NormalState = "Normal";
		private const string PointerOverState = "PointerOver";
		private const string PressedState = "Pressed";
		private const string DisabledState = "Disabled";

		private const string ActionIconPresenter = "PART_ActionIconPresenter";
		private const string HeaderPresenter = "PART_HeaderPresenter";
		private const string DescriptionPresenter = "PART_DescriptionPresenter";
		private const string HeaderIconPresenterHolder = "PART_HeaderIconPresenterHolder";

		public SettingsCard()
		{
			this.DefaultStyleKey = typeof(SettingsCard);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			IsEnabledChanged -= OnIsEnabledChanged;
			OnButtonIconChanged();
			OnHeaderChanged();
			OnHeaderIconChanged();
			OnDescriptionChanged();
			OnIsClickEnabledChanged();
			VisualStateManager.GoToState(this, IsEnabled ? NormalState : DisabledState, true);
			RegisterAutomation();
			IsEnabledChanged += OnIsEnabledChanged;
		}

		private void RegisterAutomation()
		{
			if (Header is string headerString && headerString != string.Empty)
			{
				AutomationProperties.SetName(this, headerString);
				// We don't want to override an AutomationProperties.Name that is manually set, or if the Content basetype is of type ButtonBase (the ButtonBase.Content will be used then)
				if (Content is UIElement element && string.IsNullOrEmpty(AutomationProperties.GetName(element)) && element.GetType().BaseType != typeof(ButtonBase) && element.GetType() != typeof(TextBlock))
				{
					AutomationProperties.SetName(element, headerString);
				}
			}
		}

		private void EnableButtonInteraction()
		{
			DisableButtonInteraction();

			PointerEntered += Control_PointerEntered;
			PointerExited += Control_PointerExited;
			PreviewKeyDown += Control_PreviewKeyDown;
			PreviewKeyUp += Control_PreviewKeyUp;
		}

		private void DisableButtonInteraction()
		{
			PointerEntered -= Control_PointerEntered;
			PointerExited -= Control_PointerExited;
			PreviewKeyDown -= Control_PreviewKeyDown;
			PreviewKeyUp -= Control_PreviewKeyUp;
		}

		private void Control_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.GamepadA)
			{
				VisualStateManager.GoToState(this, NormalState, true);
			}
		}

		private void Control_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.GamepadA)
			{
				VisualStateManager.GoToState(this, PressedState, true);
			}
		}

		public void Control_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			base.OnPointerExited(e);
			VisualStateManager.GoToState(this, NormalState, true);
		}

		public void Control_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			base.OnPointerEntered(e);
			VisualStateManager.GoToState(this, PointerOverState, true);
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			//  e.Handled = true;
			if (IsClickEnabled)
			{
				base.OnPointerPressed(e);
				VisualStateManager.GoToState(this, PressedState, true);
			}
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			if (IsClickEnabled)
			{
				base.OnPointerReleased(e);
				VisualStateManager.GoToState(this, NormalState, true);
			}
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new SettingsCardAutomationPeer(this);
		}

		private void OnIsClickEnabledChanged()
		{
			OnButtonIconChanged();
			if (IsClickEnabled)
			{
				EnableButtonInteraction();
			}
			else
			{
				DisableButtonInteraction();
			}
		}

		private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			VisualStateManager.GoToState(this, IsEnabled ? NormalState : DisabledState, true);
		}

		private void OnButtonIconChanged()
		{
			if (GetTemplateChild(ActionIconPresenter) is FrameworkElement buttonIconPresenter)
			{
				buttonIconPresenter.Visibility = IsClickEnabled
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		private void OnHeaderIconChanged()
		{
			if (GetTemplateChild(HeaderIconPresenterHolder) is FrameworkElement headerIconPresenter)
			{
				headerIconPresenter.Visibility = HeaderIcon != null
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		private void OnDescriptionChanged()
		{
			if (GetTemplateChild(DescriptionPresenter) is FrameworkElement descriptionPresenter)
			{
				descriptionPresenter.Visibility = Description != null
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}

		private void OnHeaderChanged()
		{
			if (GetTemplateChild(HeaderPresenter) is FrameworkElement headerPresenter)
			{
				headerPresenter.Visibility = Header != null
					? Visibility.Visible
					: Visibility.Collapsed;
			}
		}
	}

	public partial class SettingsCard : ButtonBase
	{
		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Header"/> property.
		/// </summary>
		public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
			nameof(Header),
			typeof(object),
			typeof(SettingsCard),
			new PropertyMetadata(defaultValue: null, (d, e) => ((SettingsCard)d).OnHeaderPropertyChanged((object)e.OldValue, (object)e.NewValue)));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Description"/> property.
		/// </summary>
		public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
			nameof(Description),
			typeof(object),
			typeof(SettingsCard),
			new PropertyMetadata(defaultValue: null, (d, e) => ((SettingsCard)d).OnDescriptionPropertyChanged((object)e.OldValue, (object)e.NewValue)));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="HeaderIcon"/> property.
		/// </summary>
		public static readonly DependencyProperty HeaderIconProperty = DependencyProperty.Register(
			nameof(HeaderIcon),
			typeof(IconElement),
			typeof(SettingsCard),
			new PropertyMetadata(defaultValue: null, (d, e) => ((SettingsCard)d).OnHeaderIconPropertyChanged((IconElement)e.OldValue, (IconElement)e.NewValue)));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ActionIcon"/> property.
		/// </summary>
		public static readonly DependencyProperty ActionIconProperty = DependencyProperty.Register(
			nameof(ActionIcon),
			typeof(IconElement),
			typeof(SettingsCard),
			new PropertyMetadata(defaultValue: "\ue974"));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ActionIconToolTip"/> property.
		/// </summary>
		public static readonly DependencyProperty ActionIconToolTipProperty = DependencyProperty.Register(
			nameof(ActionIconToolTip),
			typeof(string),
			typeof(SettingsCard),
			new PropertyMetadata(defaultValue: "More"));

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="IsClickEnabled"/> property.
		/// </summary>
		public static readonly DependencyProperty IsClickEnabledProperty = DependencyProperty.Register(
			nameof(IsClickEnabled),
			typeof(bool),
			typeof(SettingsCard),
			new PropertyMetadata(defaultValue: false, (d, e) => ((SettingsCard)d).OnIsClickEnabledPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));


		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ContentAlignment"/> property.
		/// </summary>
		public static readonly DependencyProperty ContentAlignmentProperty = DependencyProperty.Register(
			nameof(ContentAlignment),
			typeof(ContentAlignment),
			typeof(SettingsCard),
			new PropertyMetadata(defaultValue: ContentAlignment.Right));

		/// <summary>
		/// Gets or sets the Header.
		/// </summary>
		public object Header
		{
			get => (object)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		/// <summary>
		/// Gets or sets the description.
		/// </summary>
		public object Description
		{
			get => (object)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		/// <summary>
		/// Gets or sets the icon on the left.
		/// </summary>
		public IconElement HeaderIcon
		{
			get => (IconElement)GetValue(HeaderIconProperty);
			set => SetValue(HeaderIconProperty, value);
		}

		/// <summary>
		/// Gets or sets the icon that is shown when IsClickEnabled is set to true.
		/// </summary>
		public IconElement ActionIcon
		{
			get => (IconElement)GetValue(ActionIconProperty);
			set => SetValue(ActionIconProperty, value);
		}

		/// <summary>
		/// Gets or sets the tooltip of the ActionIcon.
		/// </summary>
		public string ActionIconToolTip
		{
			get => (string)GetValue(ActionIconToolTipProperty);
			set => SetValue(ActionIconToolTipProperty, value);
		}

		/// <summary>
		/// Gets or sets if the card can be clicked.
		/// </summary>
		public bool IsClickEnabled
		{
			get => (bool)GetValue(IsClickEnabledProperty);
			set => SetValue(IsClickEnabledProperty, value);
		}

		/// <summary>
		/// Gets or sets the alignment of the Content
		/// </summary>
		public ContentAlignment ContentAlignment
		{
			get => (ContentAlignment)GetValue(ContentAlignmentProperty);
			set => SetValue(ContentAlignmentProperty, value);
		}

		protected virtual void OnIsClickEnabledPropertyChanged(bool oldValue, bool newValue)
		{
			OnIsClickEnabledChanged();
		}
		protected virtual void OnHeaderIconPropertyChanged(IconElement oldValue, IconElement newValue)
		{
			OnHeaderIconChanged();
		}

		protected virtual void OnHeaderPropertyChanged(object oldValue, object newValue)
		{
			OnHeaderChanged();
		}

		protected virtual void OnDescriptionPropertyChanged(object oldValue, object newValue)
		{
			OnDescriptionChanged();
		}
	}

	public enum ContentAlignment
	{
		/// <summary>
		/// The Content is aligned to the right. Default state.
		/// </summary>
		Right,
		/// <summary>
		/// The Content is left-aligned while the Header, HeaderIcon and Description are collapsed. This is commonly used for Content types such as CheckBoxes, RadioButtons and custom layouts.
		/// </summary>
		Left,
		/// <summary>
		/// The Content is vertically aligned.
		/// </summary>
		Vertical
	}

	public class SettingsCardAutomationPeer : FrameworkElementAutomationPeer
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SettingsCard"/> class.
		/// </summary>
		/// <param name="owner">SettingsCard</param>
		public SettingsCardAutomationPeer(SettingsCard owner)
			: base(owner)
		{
		}

		/// <summary>
		/// Gets the control type for the element that is associated with the UI Automation peer.
		/// </summary>
		/// <returns>The control type.</returns>
		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Group;
		}

		/// <summary>
		/// Called by GetClassName that gets a human readable name that, in addition to AutomationControlType,
		/// differentiates the control represented by this AutomationPeer.
		/// </summary>
		/// <returns>The string that contains the name.</returns>
		protected override string GetClassNameCore()
		{
			string classNameCore = Owner.GetType().Name;
			return classNameCore;
		}
	}
}
