using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Views
{
	internal class NavigationInteractionTracker
	{
		public bool CanGoForward
		{
			get
			{
				_props.TryGetBoolean("CanGoForward", out bool val);
				return val;
			}
			set
			{
				_props.InsertBoolean("CanGoForward", value);
			}
		}

		public bool CanGoBack
		{
			get
			{
				_props.TryGetBoolean("CanGoBack", out bool val);
				return val;
			}
			set
			{
				_props.InsertBoolean("CanGoBack", value);
			}
		}

		private UIElement _rootElement;
		private UIElement _backIcon;
		private UIElement _forwardIcon;

		private Visual _rootVisual;
		private Visual _backVisual;
		private Visual _forwardVisual;

		private InteractionTracker _tracker = null!;
		private VisualInteractionSource _source = null!;
		private InteractionTrackerOwner _trackerOwner = null!;
		private CompositionPropertySet _props = null!;

		public event EventHandler<SwipeNavigationEventArgs>? NavigationRequested;

		public NavigationInteractionTracker(UIElement rootElement, UIElement backIcon, UIElement forwardIcon)
		{
			_rootElement = rootElement;
			_backIcon = backIcon;
			_forwardIcon = forwardIcon;

			ElementCompositionPreview.SetIsTranslationEnabled(_backIcon, true);
			ElementCompositionPreview.SetIsTranslationEnabled(_forwardIcon, true);
			_rootVisual = ElementCompositionPreview.GetElementVisual(_rootElement);
			_backVisual = ElementCompositionPreview.GetElementVisual(_backIcon);
			_forwardVisual = ElementCompositionPreview.GetElementVisual(_forwardIcon);

			_props = _rootVisual.Compositor.CreatePropertySet();
			_props.InsertBoolean("CanGoForward", false);
			_props.InsertBoolean("CanGoBack", false);

			rootElement.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(PointerPressed), true);

			SetupInteractionTracker();
			SetupAnimations();
		}

		private void SetupInteractionTracker()
		{
			var compositor = _rootVisual.Compositor;

			_trackerOwner = new(this);
			_tracker = InteractionTracker.CreateWithOwner(compositor, _trackerOwner);
			_tracker.MinPosition = new Vector3(-96f);
			_tracker.MaxPosition = new Vector3(96f);

			_source = VisualInteractionSource.Create(_rootVisual);
			_source.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;
			_source.PositionXSourceMode = InteractionSourceMode.EnabledWithoutInertia;
			_source.PositionXChainingMode = InteractionChainingMode.Auto;
			_source.PositionYSourceMode = InteractionSourceMode.Disabled;
			_tracker.InteractionSources.Add(_source);
		}

		private void SetupAnimations()
		{
			var compositor = _rootVisual.Compositor;

			var backResistance = CompositionConditionalValue.Create(compositor);
			var backResistanceCondition = compositor.CreateExpressionAnimation("tracker.Position.X < 0 && tracker.Position.X > -96");
			backResistanceCondition.SetReferenceParameter("tracker", _tracker);
			var backResistanceValue = compositor.CreateExpressionAnimation("source.DeltaPosition.X * (1 - sqrt(1 - square((tracker.Position.X / -96) - 1)))");
			backResistanceValue.SetReferenceParameter("source", _source);
			backResistanceValue.SetReferenceParameter("tracker", _tracker);
			backResistance.Condition = backResistanceCondition;
			backResistance.Value = backResistanceValue;

			var forwardResistance = CompositionConditionalValue.Create(compositor);
			var forwardResistanceCondition = compositor.CreateExpressionAnimation("tracker.Position.X > 0 && tracker.Position.X < 96");
			forwardResistanceCondition.SetReferenceParameter("tracker", _tracker);
			var forwardResistanceValue = compositor.CreateExpressionAnimation("source.DeltaPosition.X * (1 - sqrt(1 - square((tracker.Position.X / 96) - 1)))");
			forwardResistanceValue.SetReferenceParameter("source", _source);
			forwardResistanceValue.SetReferenceParameter("tracker", _tracker);
			forwardResistance.Condition = forwardResistanceCondition;
			forwardResistance.Value = forwardResistanceValue;

			List<CompositionConditionalValue> conditionalValues = new() { backResistance, forwardResistance };
			_source.ConfigureDeltaPositionXModifiers(conditionalValues);

			var backAnim = compositor.CreateExpressionAnimation("props.CanGoBack ? (-clamp(tracker.Position.X, -96, 0) * 2) - 48 : -48");
			backAnim.SetReferenceParameter("tracker", _tracker);
			backAnim.SetReferenceParameter("props", _props);
			_backVisual.StartAnimation("Translation.X", backAnim);

			var forwardAnim = compositor.CreateExpressionAnimation("props.CanGoForward ? (-clamp(tracker.Position.X, 0, 96) * 2) + 48 : 48");
			forwardAnim.SetReferenceParameter("tracker", _tracker);
			forwardAnim.SetReferenceParameter("props", _props);
			_forwardVisual.StartAnimation("Translation.X", forwardAnim);
		}

		private void PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				_source.TryRedirectForManipulation(e.GetCurrentPoint(_rootElement));
			}
		}

		private class InteractionTrackerOwner : IInteractionTrackerOwner
		{
			private NavigationInteractionTracker _parent;
			private bool _shouldBounceBack;

			public InteractionTrackerOwner(NavigationInteractionTracker parent)
			{
				_parent = parent;
			}

			public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
			{

			}

			public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
			{
				if (!_shouldBounceBack) return;

				var compositor = _parent._rootVisual.Compositor;
				var springAnim = compositor.CreateSpringVector3Animation();
				springAnim.FinalValue = new(0f);
				springAnim.DampingRatio = 1f;
				_parent._tracker.TryUpdatePositionWithAnimation(springAnim);
				_shouldBounceBack = false;

				if (Math.Abs(sender.Position.X) < 64)
					return;

				EventHandler<SwipeNavigationEventArgs>? navEvent = _parent.NavigationRequested;
				if (navEvent is null)
					return;

				navEvent(_parent, sender.Position.X > 0 ? SwipeNavigationEventArgs.Forward : SwipeNavigationEventArgs.Back);
			}

			public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
			{

			}

			public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
			{
				_shouldBounceBack = true;
			}

			public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
			{

			}

			public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
			{

			}
		}
	}

	public enum SwipeNavigationEventArgs
	{
		Back,
		Forward
	}
}
