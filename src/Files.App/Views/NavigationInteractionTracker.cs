﻿using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Files.App.Views
{
	internal class NavigationInteractionTracker : IDisposable
	{
		public bool CanNavigateForward
		{
			get
			{
				_props.TryGetBoolean(nameof(CanNavigateForward), out bool val);
				return val;
			}
			set
			{
				_props.InsertBoolean(nameof(CanNavigateForward), value);
				_tracker.MaxPosition = new(value ? 108f : 0f);
			}
		}

		public bool CanNavigateBackward
		{
			get
			{
				_props.TryGetBoolean(nameof(CanNavigateBackward), out bool val);
				return val;
			}
			set
			{
				_props.InsertBoolean(nameof(CanNavigateBackward), value);
				_tracker.MinPosition = new(value ? -108f : 0f);
			}
		}

		private UIElement _rootElement;
		private UIElement _backIcon;
		private UIElement _forwardIcon;

		private PointerEventHandler _pointerPressedHandler;

		private Visual _rootVisual;
		private Visual _backVisual;
		private Visual _forwardVisual;

		private InteractionTracker _tracker;
		private VisualInteractionSource _source;
		private InteractionTrackerOwner _trackerOwner;
		private CompositionPropertySet _props;

		public event EventHandler<OverscrollNavigationEventArgs>? NavigationRequested;

		private bool _disposed;

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

			SetupInteractionTracker();

			_props = _rootVisual.Compositor.CreatePropertySet();
			CanNavigateBackward = false;
			CanNavigateForward = false;

			SetupAnimations();

			_pointerPressedHandler = new(PointerPressed);
			_rootElement.AddHandler(UIElement.PointerPressedEvent, _pointerPressedHandler, true);
		}

		[MemberNotNull(nameof(_tracker), nameof(_source), nameof(_trackerOwner))]
		private void SetupInteractionTracker()
		{
			var compositor = _rootVisual.Compositor;

			_trackerOwner = new(this);
			_tracker = InteractionTracker.CreateWithOwner(compositor, _trackerOwner);
			_tracker.MinPosition = new Vector3(-108f);
			_tracker.MaxPosition = new Vector3(108f);

			_source = VisualInteractionSource.Create(_rootVisual);
			_source.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;
			_source.PositionXSourceMode = InteractionSourceMode.EnabledWithoutInertia;
			_source.PositionXChainingMode = InteractionChainingMode.Always;
			_source.PositionYSourceMode = InteractionSourceMode.Disabled;
			_tracker.InteractionSources.Add(_source);
		}

		private void SetupAnimations()
		{
			var compositor = _rootVisual.Compositor;

			var backResistance = CreateResistanceCondition(-108f, 0f);
			var forwardResistance = CreateResistanceCondition(0f, 108f);
			List<CompositionConditionalValue> conditionalValues = new() { backResistance, forwardResistance };
			_source.ConfigureDeltaPositionXModifiers(conditionalValues);

			var backAnim = compositor.CreateExpressionAnimation("(-clamp(tracker.Position.X, -96, 0) * 2) - 48");
			backAnim.SetReferenceParameter("tracker", _tracker);
			backAnim.SetReferenceParameter("props", _props);
			_backVisual.StartAnimation("Translation.X", backAnim);

			var forwardAnim = compositor.CreateExpressionAnimation("(-clamp(tracker.Position.X, 0, 96) * 2) + 48");
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

		private CompositionConditionalValue CreateResistanceCondition(float minValue, float maxValue)
		{
			var compositor = _rootVisual.Compositor;

			var resistance = CompositionConditionalValue.Create(compositor);
			var resistanceCondition = compositor.CreateExpressionAnimation($"tracker.Position.X > {minValue} && tracker.Position.X < {maxValue}");
			resistanceCondition.SetReferenceParameter("tracker", _tracker);
			var resistanceValue = compositor.CreateExpressionAnimation($"source.DeltaPosition.X * (1 - sqrt(1 - square((tracker.Position.X / {minValue + maxValue}) - 1)))");
			resistanceValue.SetReferenceParameter("source", _source);
			resistanceValue.SetReferenceParameter("tracker", _tracker);
			resistance.Condition = resistanceCondition;
			resistance.Value = resistanceValue;

			return resistance;
		}

		~NavigationInteractionTracker()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_rootElement.RemoveHandler(UIElement.PointerPressedEvent, _pointerPressedHandler);
			_backVisual.StopAnimation("Translation.X");
			_forwardVisual.StopAnimation("Translation.X");
			_tracker.Dispose();
			_source.Dispose();
			_props.Dispose();

			_disposed = true;
			GC.SuppressFinalize(this);
		}

		private class InteractionTrackerOwner : IInteractionTrackerOwner
		{
			private NavigationInteractionTracker _parent;
			private bool _shouldBounceBack;

			public InteractionTrackerOwner(NavigationInteractionTracker parent)
			{
				_parent = parent;
			}

			public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
			{
				if (!_shouldBounceBack)
					return;

				if (Math.Abs(sender.Position.X) > 64)
				{
					_parent._tracker.TryUpdatePosition(new(0f));

					EventHandler<OverscrollNavigationEventArgs>? navEvent = _parent.NavigationRequested;
					if (navEvent is not null)
					{
						if (sender.Position.X > 0 && _parent.CanNavigateForward)
						{
							navEvent(_parent, OverscrollNavigationEventArgs.Forward);
						}
						else if (sender.Position.X < 0 && _parent.CanNavigateBackward)
						{
							navEvent(_parent, OverscrollNavigationEventArgs.Back);
						}
					}
				}
				else
				{
					var compositor = _parent._rootVisual.Compositor;
					var springAnim = compositor.CreateSpringVector3Animation();
					springAnim.FinalValue = new(0f);
					springAnim.DampingRatio = 1f;
					_parent._tracker.TryUpdatePositionWithAnimation(springAnim);
					springAnim.Dispose();
				}
				_shouldBounceBack = false;
			}

			public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
			{
				_shouldBounceBack = true;
			}

			// required to implement IInteractionTrackerOwner
			public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args) { }
			public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args) { }
			public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args) { }
			public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args) { }
		}
	}

	public enum OverscrollNavigationEventArgs
	{
		Back,
		Forward
	}
}
