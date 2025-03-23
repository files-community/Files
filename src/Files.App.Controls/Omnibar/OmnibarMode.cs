// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	[DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
	public partial class OmnibarMode : Control
	{
		// Constants

		private const string TemplatePartName_ModeButton = "PART_ModeButton";

		// Fields

		private WeakReference<Omnibar>? _ownerRef;

		private Border? _modeButton;

		// Constructor

		public OmnibarMode()
		{
			DefaultStyleKey = typeof(OmnibarMode);
		}

		// Methods

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_modeButton = GetTemplateChild(TemplatePartName_ModeButton) as Border
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ModeButton} in the given {nameof(OmnibarMode)}'s style.");
			
			Loaded += OmnibarMode_Loaded;
			_modeButton.PointerEntered += ModeButton_PointerEntered;
			_modeButton.PointerPressed += ModeButton_PointerPressed;
			_modeButton.PointerReleased += ModeButton_PointerReleased;
			_modeButton.PointerExited += ModeButton_PointerExited;
		}

		private void OmnibarMode_Loaded(object sender, RoutedEventArgs e)
		{
			// Set this mode as the current mode if it is the default mode
			if (IsDefault && _ownerRef is not null && _ownerRef.TryGetTarget(out var owner))
				owner.ChangeMode(this);
		}

		public void SetOwner(Omnibar owner)
		{
			_ownerRef = new(owner);
		}

		public override string ToString()
		{
			return ModeName ?? string.Empty;
		}
	}
}
