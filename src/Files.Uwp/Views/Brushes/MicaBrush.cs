using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.System;
using Windows.System.Power;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Input;
using Windows.UI.Input.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.Foundation.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Graphics.Canvas.Effects;

using GuidAttribute = System.Runtime.InteropServices.GuidAttribute;

namespace Files.Uwp.Views.Brushes
{
    public class MicaBrush : XamlCompositionBrushBase
    {
        public MicaBrush(bool checkForCompositionSupport = true)
        {
            DispatcherQueue = DispatcherQueue.GetForCurrentThread();

            FallbackColor = Colors.Transparent;

            _wallpaperBrushSupported = checkForCompositionSupport ? ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "TryCreateBlurredWallpaperBackdropBrush") : true;
        }

        [ComImport]
        [Guid("0D8FB190-F122-5B8D-9FDD-543B0D8EB7F3")]
        [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
        interface ICompositorWithBlurredWallpaperBackdropBrush
        {
            CompositionBackdropBrush TryCreateBlurredWallpaperBackdropBrush();
        }

        public void SetAppWindow(AppWindow window)
        {
            if (_window != null)
            {
                _window.Changed -= AppWindow_Changed;
            }

            if (_appWindowActivationListener != null)
            {
                _appWindowActivationListener.InputActivationChanged -= AppWindow_InputActivationChanged;
                _appWindowActivationListener.Dispose();
            }

            _window = window;

            window.Changed += AppWindow_Changed;
        }

        public static CompositionBrush BuildMicaEffectBrush(Compositor compositor, Color tintColor, float tintOpacity, float luminosityOpacity)
        {
            // Tint Color.

            var tintColorEffect = new ColorSourceEffect();
            tintColorEffect.Name = "TintColor";
            tintColorEffect.Color = tintColor;

            // OpacityEffect applied to Tint.
            var tintOpacityEffect = new OpacityEffect();
            tintOpacityEffect.Name = "TintOpacity";
            tintOpacityEffect.Opacity = tintOpacity;
            tintOpacityEffect.Source = tintColorEffect;

            // Apply Luminosity:

            // Luminosity Color.
            var luminosityColorEffect = new ColorSourceEffect();
            luminosityColorEffect.Color = tintColor;

            // OpacityEffect applied to Luminosity.
            var luminosityOpacityEffect = new OpacityEffect();
            luminosityOpacityEffect.Name = "LuminosityOpacity";
            luminosityOpacityEffect.Opacity = luminosityOpacity;
            luminosityOpacityEffect.Source = luminosityColorEffect;

            // Luminosity Blend.
            var luminosityBlendEffect = new BlendEffect();
            luminosityBlendEffect.Mode = BlendEffectMode.Color;
            luminosityBlendEffect.Background = new CompositionEffectSourceParameter("BlurredWallpaperBackdrop");
            luminosityBlendEffect.Foreground = luminosityOpacityEffect;

            // Apply Tint:

            // Color Blend.
            var colorBlendEffect = new BlendEffect();
            colorBlendEffect.Mode = BlendEffectMode.Luminosity;
            colorBlendEffect.Background = luminosityBlendEffect;
            colorBlendEffect.Foreground = tintOpacityEffect;

            CompositionEffectBrush micaEffectBrush = compositor.CreateEffectFactory(colorBlendEffect).CreateBrush();
            var blurredWallpaperBackdropBrush = (ICompositorWithBlurredWallpaperBackdropBrush)((object)compositor);
            micaEffectBrush.SetSourceParameter("BlurredWallpaperBackdrop", blurredWallpaperBackdropBrush.TryCreateBlurredWallpaperBackdropBrush());

            return micaEffectBrush;
        }

        private CompositionBrush CreateCrossFadeEffectBrush(Compositor compositor, CompositionBrush from, CompositionBrush to)
        {
            var crossFadeEffect = new CrossFadeEffect();
            crossFadeEffect.Name = "Crossfade"; // Name to reference when starting the animation.
            crossFadeEffect.Source1 = new CompositionEffectSourceParameter("source1");
            crossFadeEffect.Source2 = new CompositionEffectSourceParameter("source2");
            crossFadeEffect.CrossFade = 0;

            CompositionEffectBrush crossFadeEffectBrush = compositor.CreateEffectFactory(crossFadeEffect, new List<string>() { "Crossfade.CrossFade" }).CreateBrush();
            crossFadeEffectBrush.Comment = "Crossfade";
            // The inputs have to be swapped here to work correctly...
            crossFadeEffectBrush.SetSourceParameter("source1", to);
            crossFadeEffectBrush.SetSourceParameter("source2", from);
            return crossFadeEffectBrush;
        }

        private ScalarKeyFrameAnimation CreateCrossFadeAnimation(Compositor compositor)
        {
            ScalarKeyFrameAnimation animation = compositor.CreateScalarKeyFrameAnimation();
            LinearEasingFunction linearEasing = compositor.CreateLinearEasingFunction();
            animation.InsertKeyFrame(0.0f, 0.0f, linearEasing);
            animation.InsertKeyFrame(1.0f, 1.0f, linearEasing);
            animation.Duration = TimeSpan.FromMilliseconds(250);
            return animation;
        }

        private void UpdateBrush()
        {
            FrameworkElement root = _root;

            if (_window != null)
            {
                root = ElementCompositionPreview.GetAppWindowContent(_window) as FrameworkElement;
            }

            if (root == null || _settings == null || _accessibilitySettings == null || _fastEffects == null || _energySaver == null)
                return;

            bool useSolidColorFallback = !_settings.AdvancedEffectsEnabled || !_wallpaperBrushSupported || !_windowActivated || _fastEffects == false || _energySaver == true;


            Compositor compositor = Window.Current.Compositor;

            ElementTheme currentTheme = root.ActualTheme;
            Color tintColor = currentTheme == ElementTheme.Light ? Color.FromArgb(255, 243, 243, 243) : Color.FromArgb(255, 32, 32, 32);
            float tintOpacity = currentTheme == ElementTheme.Light ? 0.5f : 0.8f;

            if (_accessibilitySettings.HighContrast)
            {
                tintColor = _settings.GetColorValue(UIColorType.Background);
                useSolidColorFallback = true;
            }

            FallbackColor = tintColor;

            CompositionBrush newBrush;

            if (useSolidColorFallback)
            {
                newBrush = compositor.CreateColorBrush(tintColor);
            }
            else
            {
                newBrush = BuildMicaEffectBrush(compositor, tintColor, tintOpacity, 1.0f);
            }

            CompositionBrush oldBrush = CompositionBrush;

            if (oldBrush == null || (CompositionBrush.Comment == "Crossfade") || (oldBrush is CompositionColorBrush && newBrush is CompositionColorBrush))
            {
                // Set new brush directly
                if (oldBrush != null)
                {
                    oldBrush.Dispose();
                }
                CompositionBrush = newBrush;
            }
            else
            {
                // Crossfade
                CompositionBrush crossFadeBrush = CreateCrossFadeEffectBrush(compositor, oldBrush, newBrush);
                ScalarKeyFrameAnimation animation = CreateCrossFadeAnimation(compositor);
                CompositionBrush = crossFadeBrush;

                var crossFadeAnimationBatch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
                crossFadeBrush.StartAnimation("CrossFade.CrossFade", animation);
                crossFadeAnimationBatch.End();

                crossFadeAnimationBatch.Completed += (o, a) =>
                {
                    crossFadeBrush.Dispose();
                    oldBrush.Dispose();
                    CompositionBrush = newBrush;
                };
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            // InputActivationListener.CreateForApplicationWindow throws an exception if called too early
            // Delay it until the window visibility has changed
            if (args.DidVisibilityChange && _appWindowActivationListener == null)
            {
                _appWindowActivationListener = InputActivationListenerPreview.CreateForApplicationWindow(_window);
                _windowActivated = _appWindowActivationListener.State != InputActivationState.Deactivated;
                _appWindowActivationListener.InputActivationChanged += AppWindow_InputActivationChanged;
                UpdateBrush();
            }
        }

        private void AppWindow_InputActivationChanged(InputActivationListener sender, InputActivationListenerActivationChangedEventArgs args)
        {
            _windowActivated = args.State != InputActivationState.Deactivated;
            UpdateBrush();
        }

        private void AccessibilitySettings_HighContrastChanged(AccessibilitySettings sender, object args)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                UpdateBrush();
            });
        }

        private void ColorValuesChanged(UISettings sender, object args)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                UpdateBrush();
            });
        }

        private void PowerManager_EnergySaverStatusChanged(object sender, object e)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                _energySaver = PowerManager.EnergySaverStatus == EnergySaverStatus.On;
                UpdateBrush();
            });
        }

        private void CompositionCapabilities_Changed(CompositionCapabilities sender, object args)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                _fastEffects = CompositionCapabilities.GetForCurrentView().AreEffectsFast();
                UpdateBrush();
            });
        }

        protected override void OnConnected()
        {
            base.OnConnected();

            if (DesignMode.DesignModeEnabled)
                return;

            if (_settings == null)
                _settings = new UISettings();

            if (_accessibilitySettings == null)
                _accessibilitySettings = new AccessibilitySettings();

            if (_fastEffects == null)
                _fastEffects = CompositionCapabilities.GetForCurrentView().AreEffectsFast();
            if (_energySaver == null)
                _energySaver = PowerManager.EnergySaverStatus == EnergySaverStatus.On;

            UpdateBrush();

            // Trigger event on color changes (themes). Is also triggered for advanced effects changes.
            _settings.ColorValuesChanged -= ColorValuesChanged;
            _settings.ColorValuesChanged += ColorValuesChanged;

            _accessibilitySettings.HighContrastChanged -= AccessibilitySettings_HighContrastChanged;
            _accessibilitySettings.HighContrastChanged += AccessibilitySettings_HighContrastChanged;

            PowerManager.EnergySaverStatusChanged -= PowerManager_EnergySaverStatusChanged;
            PowerManager.EnergySaverStatusChanged += PowerManager_EnergySaverStatusChanged;

            CompositionCapabilities.GetForCurrentView().Changed -= CompositionCapabilities_Changed;
            CompositionCapabilities.GetForCurrentView().Changed += CompositionCapabilities_Changed;
        }

        protected override void OnDisconnected()
        {
            base.OnDisconnected();

            if (_window != null)
            {
                _window.Changed -= AppWindow_Changed;
            }

            if (_appWindowActivationListener != null)
            {
                _appWindowActivationListener.InputActivationChanged -= AppWindow_InputActivationChanged;
                _appWindowActivationListener.Dispose();
            }

            if (_settings != null)
            {
                _settings.ColorValuesChanged -= ColorValuesChanged;
                _settings = null;
            }

            if (_accessibilitySettings != null)
            {
                _accessibilitySettings.HighContrastChanged -= AccessibilitySettings_HighContrastChanged;
                _accessibilitySettings = null;
            }

            PowerManager.EnergySaverStatusChanged -= PowerManager_EnergySaverStatusChanged;

            CompositionCapabilities.GetForCurrentView().Changed -= CompositionCapabilities_Changed;

            if (CompositionBrush != null)
            {
                CompositionBrush.Dispose();
                CompositionBrush = null;
            }
        }

        private DispatcherQueue DispatcherQueue;
        private bool _wallpaperBrushSupported = false;
        private bool? _fastEffects;
        private bool? _energySaver;
        private UISettings _settings;
        private AccessibilitySettings _accessibilitySettings;
        private bool _windowActivated;
        private AppWindow _window;
        private InputActivationListener _appWindowActivationListener;
        private FrameworkElement _root;
    }
}
